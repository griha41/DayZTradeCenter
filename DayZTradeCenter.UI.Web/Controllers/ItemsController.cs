﻿using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using DayZTradeCenter.DomainModel.Entities;
using rg.GenericRepository.Core;
using PagedList;

namespace DayZTradeCenter.UI.Web.Controllers
{
    public class ItemsController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsController"/> class.
        /// </summary>
        /// <param name="itemsRepository">The items repository.</param>
        /// <exception cref="System.ArgumentNullException">itemsRepository</exception>
        public ItemsController(IRepository<Item> itemsRepository)
        {
            if (itemsRepository == null)
            {
                throw new ArgumentNullException("itemsRepository");
            }

            _itemsRepository = itemsRepository;
        }

        // GET: Items
        public ActionResult Index(int? page)
        {
            var items = GetItemsByPage(page);

            return Request.IsAjaxRequest()
                ? (ActionResult) PartialView("_ItemsTable", items)
                : View(items);
        }

        private IPagedList<Item> GetItemsByPage(int? page)
        {
            var model =
                _itemsRepository
                    .GetAll()
                    // added to avoid "The method ‘Skip’ is only supported for sorted input in LINQ to Entities. 
                    //  The method ‘OrderBy’ must be called before the method ‘Skip’".
                    // http://stackoverflow.com/questions/21705926/the-method-skip-is-only-supported-for-sorted-input-in-linq-to-entities-the-me
                    .OrderBy(i => i.Id);

            const int pageSize = 10;
            var pageNumber = (page ?? 1);

            return model.ToPagedList(pageNumber, pageSize);
        }

        // GET: Items/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Items/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id, Name, Rarity, Details")] Item item)
        {
            if (!ModelState.IsValid)
            {
                return View(item);
            }

            _itemsRepository.Insert(item);
            _itemsRepository.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Items/Edit/5
        public ActionResult Edit(int id)
        {
            var model = _itemsRepository.GetSingle(id);

            return model == null
                ? View("NotFound")
                : View(model);
        }

        // POST: Items/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id, Name, Rarity, Details")] Item item)
        {
            if (!ModelState.IsValid)
            {
                return View(item);
            }

            var model = _itemsRepository.GetSingle(item.Id);
            model.Name = item.Name;
            model.Rarity = item.Rarity;
            model.Details = item.Details;

            _itemsRepository.Update(model);
            _itemsRepository.SaveChanges();

            return RedirectToAction("Index");
        }

        // POST: Items/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            var model = _itemsRepository.GetSingle(id);

            _itemsRepository.Delete(model);
            _itemsRepository.SaveChanges();

            return Json(new { success = true });
        }


        private readonly IRepository<Item> _itemsRepository;
    }
}