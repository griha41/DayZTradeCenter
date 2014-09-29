﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using DayZTradeCenter.DomainModel;
using DayZTradeCenter.DomainModel.Identity.Entities;
using DayZTradeCenter.DomainModel.Identity.Services;
using DayZTradeCenter.UI.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using rg.GenericRepository.Core;

namespace DayZTradeCenter.UI.Web.Controllers
{
    public class TradesController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TradesController"/> class.
        /// </summary>
        /// <param name="tradesRepository">The trades repository.</param>
        /// <param name="itemsRepository">The items repository.</param>
        /// <param name="tradeManager">The trade manager.</param>
        /// <exception cref="System.ArgumentNullException">
        /// tradesRepository
        /// or
        /// itemsRepository
        /// or
        /// tradeManager
        /// </exception>
        public TradesController(
            IRepository<Trade> tradesRepository, IRepository<Item> itemsRepository, ITradeManager tradeManager)
        {
            if (tradesRepository == null)
            {
                throw new ArgumentNullException("tradesRepository");
            }

            if (itemsRepository == null)
            {
                throw new ArgumentNullException("itemsRepository");
            }

            if (tradeManager == null)
            {
                throw new ArgumentNullException("tradeManager");
            }

            _tradesRepository = tradesRepository;
            _itemsRepository = itemsRepository;
            _tradeManager = tradeManager;
        }

        /// <summary>
        /// Gets the user manager.
        /// </summary>
        /// <value>
        /// The user manager.
        /// </value>
        public ApplicationUserManager UserManager
        {
            get
            {
                return HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
        }

        // GET: Trades
        public ActionResult Index()
        {
            var model = _tradesRepository.GetAll();

            var userId = User.Identity.GetUserId();

            var vm = new ListTradesViewModel(
                !User.IsInRole("Administrator"),
                userId,
                model,
                _tradeManager.CanCreateTrade(userId));

            return View(vm);
        }

        // GET: Trades/Create
        public ActionResult Create()
        {
            var items = _itemsRepository.GetAll();

            ViewBag.Items =
                items.Select(item => new {item.Id, item.Name});

            return View();
        }

        // POST: Trades/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Create(CreateTradeViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return Json(new
                {
                    success = false,
                    error = "It is not possible to create a trade for the same items."
                });
            }

            var trade = new Trade();
            foreach (var itemDetails in vm.Have)
            {
                trade.Have.Add(
                    new TradeDetails(_itemsRepository.GetSingle(itemDetails.Id), itemDetails.Quantity));
            }
            foreach (var itemDetails in vm.Want)
            {
                trade.Want.Add(
                    new TradeDetails(_itemsRepository.GetSingle(itemDetails.Id), itemDetails.Quantity));
            }

            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            trade.Owner = user;

            // TODO: use TimeProvider
            trade.CreationDate = DateTime.Now;

            _tradesRepository.Insert(trade);
            _tradesRepository.SaveChanges();

            return Json(new {success = true});
        }

        // GET: Trades/Offer
        public async Task<ActionResult> Offer(int tradeId)
        {
            var trade = _tradesRepository.GetSingle(tradeId);
            
            var userId = User.Identity.GetUserId();
            var user = await UserManager.FindByIdAsync(userId);

            // user has not yet offered for this trade
            if (trade.Offers.All(o => o.Id != user.Id))
            {
                trade.Offers.Add(user);

                _tradesRepository.Update(trade);
                _tradesRepository.SaveChanges();

                return RedirectToAction("Index");
            }

            return View("AlreadyOffered");
        }

        // GET: Trades/Withdraw
        public ActionResult Withdraw(int tradeId)
        {
            var trade = _tradesRepository.GetSingle(tradeId);

            var userId = User.Identity.GetUserId();
            var user = trade.Offers.FirstOrDefault(o => o.Id == userId);

            trade.Offers.Remove(user);
            
            _tradesRepository.Update(trade);
            _tradesRepository.SaveChanges();

            return RedirectToAction("Index");
        }

        // GET: Trades/Details/5
        public ActionResult Details(int id)
        {
            var model = _tradesRepository.GetSingle(id);

            return View(model);
        }

        // GET: Trades/ChooseWinner/tradeId=1&userId=2
        public ActionResult ChooseWinner(int tradeId, string userId)
        {
            var model = _tradesRepository.GetSingle(tradeId);

            model.Winner = userId;

            _tradesRepository.Update(model);
            _tradesRepository.SaveChanges();

            return RedirectToAction("Edit", "Profile");
        }

        // GET: Trades/ExchangeManagement/5
        public ActionResult ExchangeManagement(int id)
        {
            var model = new ExchangeManagementViewModel
            {
                Trade = _tradesRepository.GetSingle(id)
            };
            
            return View(model);
        }

        // POST: Trades/ExchangeManagement/5
        [HttpPost, ActionName("ExchangeManagement")]
        [ValidateAntiForgeryToken]
        public ActionResult ExchangeManagementConfirmed(ExchangeDetails details, Trade trade)
        {
            var message = new Message(
                string.Format("My SteamId is {0}. Meet me at {1}, server {2}, time {3} GTM",
                    details.SteamId,
                    details.Location,
                    details.Server,
                    details.Time));

            var model = new ExchangeManagementViewModel {Trade = trade};
            model.Messages.Add(message);

            return View("ExchangeManagement", model);
        }

        // POST: Trades/TradeCompleted/1
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> TradeCompleted(int id)
        {
            var model = _tradesRepository.GetSingle(id);
            model.Completed = true;

            //var user = await UserManager.FindByIdAsync(model.Winner);
            var user = await UserManager.FindByIdAsync("bfc71c53-fc4b-4061-9459-0940f92e764d");

            if (user.Messages == null)
            {
                user.Messages = new List<Message>();
            }

            var message = new FeedbackRequestMessage {TradeId = model.Id};
            user.Messages.Add(message);

            await UserManager.UpdateAsync(user);

            _tradesRepository.Update(model);
            _tradesRepository.SaveChanges();

            return View(model);
        }

        [ActionName("TradeCompleted")]
        public ActionResult TradeCompletedGet(int id)
        {
            var model = _tradesRepository.GetSingle(id);
            
            return View(model);
        }

        public async Task<ActionResult> LeaveFeedback(int id, int score)
        {
            var model = _tradesRepository.GetSingle(id);

            //var user = await UserManager.FindByIdAsync(model.Winner);
            var user = await UserManager.FindByIdAsync("bfc71c53-fc4b-4061-9459-0940f92e764d");
            
            if (user.Feedbacks == null)
            {
                user.Feedbacks = new List<Feedback>();
            }

            user.Feedbacks.Add(new Feedback
            {
                From = model.Owner.Id,
                Timestamp = DateTime.Now,
                Score = score,
                TradeId = id
            });

            await UserManager.UpdateAsync(user);

            return View();
        }

        #region Private fields

        private readonly IRepository<Trade> _tradesRepository;
        private readonly IRepository<Item> _itemsRepository;
        private readonly ITradeManager _tradeManager;

        #endregion
    }
}