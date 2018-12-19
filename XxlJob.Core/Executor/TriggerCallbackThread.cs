using com.xxl.job.core.biz.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XxlJob.Core.Executor
{
    internal class TriggerCallbackThread
    {
        public static Lazy<TriggerCallbackThread> Instance = new Lazy<TriggerCallbackThread>(LazyThreadSafetyMode.ExecutionAndPublication);

        public static void PushCallBack(HandleCallbackParam callback)
        {
            Instance.Value.callBackQueue.Enqueue(callback);
        }


        private ConcurrentQueue<HandleCallbackParam> callBackQueue = new ConcurrentQueue<HandleCallbackParam>();
        private Thread triggerCallbackThread;
        private Thread triggerRetryCallbackThread;
        private volatile bool toStop = false;

        public void Start()
        {
            // valid
            //if (XxlJobExecutor.getAdminBizList() == null)
            //{
            //    logger.warn(">>>>>>>>>>> xxl-job, executor callback config fail, adminAddresses is null.");
            //    return;
            //}

            triggerCallbackThread = new Thread(CallbackMethod);
            triggerRetryCallbackThread = new Thread(RetryMethod);

            triggerCallbackThread.Start();
            triggerRetryCallbackThread.Start();
        }

        public void ToStop()
        {
            toStop = true;
            // stop callback, interrupt and wait
            triggerCallbackThread.Interrupt();
            try
            {
                triggerCallbackThread.Join();
            }
            catch (ThreadInterruptedException ex)
            {
                //logger.error(e.getMessage(), ex);
            }

            // stop retry, interrupt and wait
            triggerRetryCallbackThread.Interrupt();
            try
            {
                triggerRetryCallbackThread.Join();
            }
            catch (ThreadInterruptedException ex)
            {
                //logger.error(e.getMessage(), ex);
            }
        }

        private void CallbackMethod()
        {
            //// normal callback
            //while (!toStop)
            //{
            //    try
            //    {
            //        HandleCallbackParam callback = Instance.Value.callBackQueue.take();
            //        if (callback != null)
            //        {

            //            // callback list param
            //            var callbackParamList = new List<HandleCallbackParam>();                        
            //            int drainToNum = getInstance().callBackQueue.drainTo(callbackParamList);
            //            callbackParamList.add(callback);

            //            // callback, will retry if error
            //            if (callbackParamList != null && callbackParamList.size() > 0)
            //            {
            //                doCallback(callbackParamList);
            //            }
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        //logger.error(e.getMessage(), e);
            //    }
            //}

            //// last callback
            //try
            //{
            //    List<HandleCallbackParam> callbackParamList = new ArrayList<HandleCallbackParam>();
            //    int drainToNum = getInstance().callBackQueue.drainTo(callbackParamList);
            //    if (callbackParamList != null && callbackParamList.size() > 0)
            //    {
            //        doCallback(callbackParamList);
            //    }
            //}
            //catch (Exception e)
            //{
            //    //logger.error(e.getMessage(), e);
            //}
            ////logger.info(">>>>>>>>>>> xxl-job, executor callback thread destory.");
        }

        private void RetryMethod()
        {
            //while (!toStop)
            //{
            //    try
            //    {
            //        retryFailCallbackFile();
            //    }
            //    catch (Exception e)
            //    {
            //        logger.error(e.getMessage(), e);
            //    }
            //    try
            //    {
            //        TimeUnit.SECONDS.sleep(RegistryConfig.BEAT_TIMEOUT);
            //    }
            //    catch (InterruptedException e)
            //    {
            //        logger.warn(">>>>>>>>>>> xxl-job, executor retry callback thread interrupted, error msg:{}", e.getMessage());
            //    }
            //}
            //logger.info(">>>>>>>>>>> xxl-job, executor retry callback thread destory.");
        }

        private void doCallback(List<HandleCallbackParam> callbackParamList)
        {
            //boolean callbackRet = false;
            //// callback, will retry if error
            //for (AdminBiz adminBiz: XxlJobExecutor.getAdminBizList())
            //{
            //    try
            //    {
            //        ReturnT<String> callbackResult = adminBiz.callback(callbackParamList);
            //        if (callbackResult != null && ReturnT.SUCCESS_CODE == callbackResult.getCode())
            //        {
            //            callbackLog(callbackParamList, "<br>----------- xxl-job job callback finish.");
            //            callbackRet = true;
            //            break;
            //        }
            //        else
            //        {
            //            callbackLog(callbackParamList, "<br>----------- xxl-job job callback fail, callbackResult:" + callbackResult);
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        callbackLog(callbackParamList, "<br>----------- xxl-job job callback error, errorMsg:" + e.getMessage());
            //    }
            //}
            //if (!callbackRet)
            //{
            //    appendFailCallbackFile(callbackParamList);
            //}
        }

        private void callbackLog(List<HandleCallbackParam> callbackParamList, String logContent)
        {
            //for (HandleCallbackParam callbackParam: callbackParamList)
            //{
            //    String logFileName = XxlJobFileAppender.makeLogFileName(new Date(callbackParam.getLogDateTim()), callbackParam.getLogId());
            //    XxlJobFileAppender.contextHolder.set(logFileName);
            //    XxlJobLogger.log(logContent);
            //}
        }


        /*
    // ---------------------- fail-callback file ----------------------

    private static String failCallbackFileName = XxlJobFileAppender.getLogPath().concat(File.separator).concat("xxl-job-callback").concat(".log");

    private void appendFailCallbackFile(List<HandleCallbackParam> callbackParamList){
        // append file
        String content = JacksonUtil.writeValueAsString(callbackParamList);
        FileUtil.appendFileLine(failCallbackFileName, content);
    }

    private void retryFailCallbackFile(){

        // load and clear file
        List<String> fileLines = FileUtil.loadFileLines(failCallbackFileName);
        FileUtil.deleteFile(failCallbackFileName);

        // parse
        List<HandleCallbackParam> failCallbackParamList = new ArrayList<>();
        if (fileLines!=null && fileLines.size()>0) {
            for (String line: fileLines) {
                List<HandleCallbackParam> failCallbackParamListTmp = JacksonUtil.readValue(line, List.class, HandleCallbackParam.class);
                if (failCallbackParamListTmp!=null && failCallbackParamListTmp.size()>0) {
                    failCallbackParamList.addAll(failCallbackParamListTmp);
                }
            }
        }

        // retry callback, 100 lines per page
        if (failCallbackParamList!=null && failCallbackParamList.size()>0) {
            int pagesize = 100;
            List<HandleCallbackParam> pageData = new ArrayList<>();
            for (int i = 0; i < failCallbackParamList.size(); i++) {
                pageData.add(failCallbackParamList.get(i));
                if (i>0 && i%pagesize == 0) {
                    doCallback(pageData);
                    pageData.clear();
                }
            }
            if (pageData.size() > 0) {
                doCallback(pageData);
            }
        }
    }
    */
    }

    internal class HandleCallbackParam
    {
        public int LogId { get; set; }

        public long LogDateTim { get; set; }

        private ReturnT ExecuteResult { get; set; }
    }
}
