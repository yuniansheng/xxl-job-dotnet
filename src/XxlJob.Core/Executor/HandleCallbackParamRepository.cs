using com.xxl.job.core.biz.model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    internal class HandleCallbackParamRepository
    {
        private readonly IOptions<JobExecutorOption> _executorOption;
        private readonly string _callbackSavePath;
        private readonly JsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        public HandleCallbackParamRepository(IOptions<JobExecutorOption> executorOption, ILoggerFactory loggerFactory)
        {
            _executorOption = executorOption;
            _callbackSavePath = Path.Combine(_executorOption.Value.LogPath, "xxl-job-callback.log");
            var dir = Path.GetDirectoryName(_callbackSavePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _jsonSerializer = JsonSerializer.Create();
            _logger = loggerFactory.CreateLogger<HandleCallbackParamRepository>();
        }


        public void SaveCallbackParams(IEnumerable<HandleCallbackParam> callbackParamList)
        {
            if (!callbackParamList.Any())
            {
                return;
            }

            try
            {
                using (var writer = new StreamWriter(_callbackSavePath, true, Encoding.UTF8))
                {
                    foreach (var item in callbackParamList)
                    {
                        if (item.callbackRetryTimes >= Constants.MaxCallbackRetryTimes)
                        {
                            //记录日志并丢弃,防止重复写入文件导致文件过大
                            _logger.LogInformation("callback failed too many times and will be abandon,logId {logId}", item.logId);
                        }
                        else
                        {
                            item.callbackRetryTimes++;
                            _jsonSerializer.Serialize(writer, item);
                            writer.WriteLine();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveCallbackParams error.");
            }
        }

        public List<HandleCallbackParam> LoadCallbackParams()
        {
            var failCallbackParamList = new List<HandleCallbackParam>();

            if (!File.Exists(_callbackSavePath))
            {
                return failCallbackParamList;
            }

            // load and clear file
            var fileLines = File.ReadAllLines(_callbackSavePath);
            File.Delete(_callbackSavePath);

            for (int i = 0; i < fileLines.Length; i++)
            {
                try
                {
                    var item = JsonConvert.DeserializeObject<HandleCallbackParam>(fileLines[i]);
                    failCallbackParamList.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DeserializeObject error for line {line}", fileLines[i]);
                }
            }
            return failCallbackParamList;
        }
    }
}
