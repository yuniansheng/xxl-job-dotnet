using com.xxl.job.core.biz.model;
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

        public HandleCallbackParamRepository(JobExecutorConfig executorConfig)
        {
            _executorConfig = executorConfig;

            _callbackSavePath = Path.Combine(_executorConfig.LogPath, "xxl-job-callback.log");
            var dir = Path.GetDirectoryName(_callbackSavePath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }


        public void SaveCallbackParams(IEnumerable<HandleCallbackParam> callbackParamList)
        {
            if (!callbackParamList.Any())
            {
                return;
            }

            var builder = new StringBuilder();
            foreach (var item in callbackParamList)
            {
                try
                {
                    var line = SerializeHandleCallbackParam(item);
                    builder.AppendLine(line);
                }
                catch (Exception)
                {
                    //todo:log error
                }
            }
            if (builder.Length > 0)
            {
                try
                {
                    File.AppendAllText(_callbackSavePath, builder.ToString());
                }
                catch (Exception)
                {
                    //todo:log error
                }
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
                var item = DeserializeHandleCallbackParam(fileLines[i]);
                failCallbackParamList.Add(item);
                //if (failCallbackParamList.Count == 100 || i == fileLines.Length - 1)
                //{
                //    DoCallback(failCallbackParamList);
                //    failCallbackParamList.Clear();
                //}
            }
            return failCallbackParamList;
        }


        private string SerializeHandleCallbackParam(HandleCallbackParam param)
        {
            throw new NotImplementedException();
        }

        private HandleCallbackParam DeserializeHandleCallbackParam(string content)
        {
            throw new NotImplementedException();
        }
    }
}
