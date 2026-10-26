using Microsoft.AspNetCore.Mvc;
using PropertyAssessmentApp.Models;
using PropertyAssessmentApp.Services;

namespace PropertyAssessmentApp.Controllers;

public class OrderPaymentController : Controller
{
    private readonly IOrderPaymentService _orderPaymentService;

    public OrderPaymentController(IOrderPaymentService orderPaymentService)
    {
        _orderPaymentService = orderPaymentService;
    }

    // GET: /OrderPayment
    public IActionResult Index(string? search, bool? isActive, string? category)
    {
        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        ViewBag.Category = category;

        ViewBag.TotalCount = _orderPaymentService.GetTotalCount();
        ViewBag.ActiveCount = _orderPaymentService.GetActiveCount();
        ViewBag.AverageFee = _orderPaymentService.GetAverageFee();
        ViewBag.Categories = _orderPaymentService.GetCategories();

        var records = _orderPaymentService.GetAll(search, isActive, category);
        return View(records);
    }

    // GET: /OrderPayment/Details/{guid}
    public IActionResult Details(Guid id)
    {
        var record = _orderPaymentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }
        return View(record);
    }

    // GET: /OrderPayment/Create
    public IActionResult Create()
    {
        var model = new OrderPayment
        {
            Id = Guid.NewGuid(),
            Code = $"OP-{new Random().Next(600, 999)}",
            IsActive = true
        };
        return View(model);
    }

    // POST: /OrderPayment/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(OrderPayment model)
    {
        if (ModelState.IsValid)
        {
            _orderPaymentService.Add(model);
            TempData["SuccessMessage"] = $"Order of Payment fee item '{model.Particular}' ({model.Code}) created successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // GET: /OrderPayment/Edit/{guid}
    public IActionResult Edit(Guid id)
    {
        var record = _orderPaymentService.GetById(id);
        if (record == null)
        {
            return NotFound();
        }
        return View(record);
    }

    // POST: /OrderPayment/Edit/{guid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(Guid id, OrderPayment model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            _orderPaymentService.Update(model);
            TempData["SuccessMessage"] = $"Order of Payment fee item '{model.Particular}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        return View(model);
    }

    // POST: /OrderPayment/Delete/{guid}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(Guid id)
    {
        var record = _orderPaymentService.GetById(id);
        if (record != null)
        {
            _orderPaymentService.Delete(id);
            TempData["SuccessMessage"] = $"Order of Payment fee item '{record.Particular}' was deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
}
