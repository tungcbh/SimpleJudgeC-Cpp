using System.Diagnostics;

namespace TestCompiler.CoreJudge
{
    public class Grader
    {
        public enum Status
        {
            Success,
            CompileError,
            RuntimeError,
            TimeLimitExceeded,
            MemoryLimitExceeded
        }

        public class GradingResult
        {
            public Status GradingStatus { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }

        public async Task<GradingResult> GradeSourceFile(string filePath, string input, string expectedOutput, bool isCpp = false)
        {
            var result = new GradingResult();
            string compiler = isCpp ? "g++" : "gcc"; // Chọn compiler
            const long memoryLimit = 256 * 1024 * 1024; // 256MB giới hạn bộ nhớ

            // Biên dịch file .c hoặc .cpp
            var compileProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = compiler,
                    Arguments = $"{filePath} -o {filePath}.out",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            compileProcess.Start();
            string compileError = await compileProcess.StandardError.ReadToEndAsync();
            compileProcess.WaitForExit();

            if (compileProcess.ExitCode != 0)
            {
                result.GradingStatus = Status.CompileError;
                result.Error = compileError;
                return result;
            }

            // Chạy chương trình đã biên dịch
            var executionProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = $"{filePath}.out",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            executionProcess.Start();

            if (!string.IsNullOrEmpty(input))
            {
                await executionProcess.StandardInput.WriteLineAsync(input);
            }

            // Đặt thời gian giới hạn (timeout)
            var timeLimitTask = Task.Delay(2000);
            var processTask = Task.Run(async () =>
            {
                while (!executionProcess.HasExited)
                {
                    executionProcess.Refresh(); // Cập nhật thông tin tiến trình

                    // Chỉ kiểm tra bộ nhớ nếu tiến trình còn đang chạy
                    try
                    {
                        if (executionProcess.WorkingSet64 > memoryLimit)
                        {
                            executionProcess.Kill();
                            return (Status.MemoryLimitExceeded, "", "Memory limit exceeded.");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Xử lý trường hợp tiến trình không ở trạng thái hợp lệ
                        break;
                    }
                }

                string output = await executionProcess.StandardOutput.ReadToEndAsync();
                string error = await executionProcess.StandardError.ReadToEndAsync();
                executionProcess.WaitForExit();
                return (executionProcess.ExitCode == 0 ? Status.Success : Status.RuntimeError, output, error);
            });

            var completedTask = await Task.WhenAny(processTask, timeLimitTask);

            if (completedTask == timeLimitTask)
            {
                executionProcess.Kill();
                result.GradingStatus = Status.TimeLimitExceeded;
                return result;
            }

            var (gradingStatus, outputResult, errorResult) = await processTask;

            if (gradingStatus == Status.MemoryLimitExceeded)
            {
                result.GradingStatus = Status.MemoryLimitExceeded;
                result.Error = errorResult;
                return result;
            }

            if (gradingStatus == Status.RuntimeError)
            {
                result.GradingStatus = Status.RuntimeError;
                result.Error = errorResult;
                return result;
            }

            result.Output = outputResult;

            // So sánh kết quả với đáp án mẫu
            if (result.Output.Trim() == expectedOutput.Trim())
            {
                result.GradingStatus = Status.Success;
            }
            else
            {
                result.GradingStatus = Status.RuntimeError;
                result.Error = "Wrong output";
            }

            return result;
        }


    }
}
