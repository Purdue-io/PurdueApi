using System;
using System.Web.Http;
using System.Web.Mvc;
using PurdueIo.Areas.HelpPage.ModelDescriptions;
using PurdueIo.Areas.HelpPage.Models;

namespace PurdueIo.Areas.HelpPage.Controllers
{
    /// <summary>
    /// The controller that will handle requests for the help page.
    /// </summary>
    public class HelpController : Controller
    {
        private const string ErrorViewName = "Error";

        public HelpController()
            : this(GlobalConfiguration.Configuration)
        {
        }

        public HelpController(HttpConfiguration config)
        {
            Configuration = config;
        }

        public HttpConfiguration Configuration { get; private set; }

        public ActionResult Index()
        {
            ViewBag.DocumentationProvider = Configuration.Services.GetDocumentationProvider();
            return View(Configuration.Services.GetApiExplorer().ApiDescriptions);
        }

        public ActionResult Courses()
        {
            return View("Courses");
        }

        public ActionResult Classes()
        {
            return View("Classes");
        }

        public ActionResult Subjects()
        {
            return View("Subjects");
        }

        public ActionResult Terms()
        {
            return View("Terms");
        }

        public ActionResult Campuses()
        {
            return View("Campuses");
        }

        public ActionResult Buildings()
        {
            return View("Buildings");
        }

        public ActionResult Sections()
        {
            return View("Sections");
        }

        public ActionResult Rooms()
        {
            return View("Rooms");
        }

        public ActionResult Instructors()
        {
            return View("Instructors");
        }

        public ActionResult Meetings()
        {
            return View("Meetings");
        }

        public ActionResult Api(string apiId)
        {
            if (!String.IsNullOrEmpty(apiId))
            {
                HelpPageApiModel apiModel = Configuration.GetHelpPageApiModel(apiId);
                if (apiModel != null)
                {
                    return View(apiModel);
                }
            }

            return View(ErrorViewName);
        }

        public ActionResult ResourceModel(string modelName)
        {
            if (!String.IsNullOrEmpty(modelName))
            {
                ModelDescriptionGenerator modelDescriptionGenerator = Configuration.GetModelDescriptionGenerator();
                ModelDescription modelDescription;
                if (modelDescriptionGenerator.GeneratedModels.TryGetValue(modelName, out modelDescription))
                {
                    return View(modelDescription);
                }
            }

            return View(ErrorViewName);
        }
    }
}