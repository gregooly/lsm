using Microsoft.AspNetCore.Mvc;

namespace LaundryMS.Web.Controllers;

public class LinenInventoryController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction(nameof(LinenItemsController.Index), "LinenItems");
    }
}
