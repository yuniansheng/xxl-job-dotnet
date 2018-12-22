using com.xxl.job.core.biz.model;
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
        private readonly JobExecutorConfig _executorConfig;
        private string _callbackSavePath;
        private JsonSerializer _jsonSerializer;

        public HandleCallbackParamRepository(JobExecutorConfig executorConfig)
        {
            _executorConfig = executorConfig;

            _callbackSavePath = Path.Combine(_executorConfig.LogPath, "xxl-job-callback.log");
            var dir = Path.GetDirectoryName(_callbackSavePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _jsonSerializer = JsonSerializer.Create();
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
                            //todo:记录日志并丢弃,防止重复写入文件导致文件过大
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
            catch (Exception)
            {
                //todo:log error
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
                catch (Exception)
                {
                    //todo:log error                    
                }
            }
            return failCallbackParamList;
        }
    }
}
