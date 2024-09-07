using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TestCompiler.CoreJudge;
using TestCompiler.Models;

namespace TestCompiler.Controllers
{
    public class HomeController : Controller
    {
        private readonly Grader _grader;

        public HomeController()
        {
            _grader = new Grader();
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string sourceCode, string inputData, string expectedOutput, string language)
        {
            // Lưu source code vào file tạm thời
            string fileExtension = language == "cpp" ? ".cpp" : ".c";
            string filePath = Path.Combine(Path.GetTempPath(), "temp" + fileExtension);
            System.IO.File.WriteAllText(filePath, sourceCode);

            // Chọn ngôn ngữ
            bool isCpp = language == "cpp";

            // Gọi hàm chấm điểm
            var result = await _grader.GradeSourceFile(filePath, inputData, expectedOutput, isCpp);

            // Trả kết quả chấm về view
            ViewBag.Result = result.GradingStatus.ToString();
            ViewBag.Output = result.Output;
            ViewBag.Error = result.Error;

            return View();
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
