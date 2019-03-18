using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace RobotSideUWP
{
    public sealed partial class ReadWriteClass : Page, IDisposable
    {
        // A pointer back to the main page.  This is needed if you want to call methods in MainPage such as NotifyUser()
        private MainPage rootPage = MainPage.Current;

        // Track Read Operation
        private CancellationTokenSource ReadCancellationTokenSource;
        private Object ReadCancelLock = new Object();

        private Boolean IsReadTaskPending;
        private uint ReadBytesCounter = 0;
        DataReader DataReaderObject = null;

        // Track Write Operation
        private CancellationTokenSource WriteCancellationTokenSource;
        private Object WriteCancelLock = new Object();

        private Boolean IsWriteTaskPending;
        private uint WriteBytesCounter = 0;
        DataWriter DataWriteObject = null;
        string oneChar = "";

        // Indicate if we navigate away from this page or not.
        private Boolean IsNavigatedAway;

        public ReadWriteClass()
        {
            ReadCancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            if (ReadCancellationTokenSource != null)
            {
                ReadCancellationTokenSource.Dispose();
                ReadCancellationTokenSource = null;
            }

            if (WriteCancellationTokenSource != null)
            {
                WriteCancellationTokenSource.Dispose();
                WriteCancellationTokenSource = null;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            IsNavigatedAway = false;
            if (CommPortClass.serialPort == null)
            {
                MainPage.Current.NotifyUser("Device is not connected", NotifyType.ErrorMessage);
            }
            else
            {
                if (CommPortClass.serialPort.PortName != "")
                {
                    MainPage.Current.NotifyUser("Connected to " + CommPortClass.serialPort.PortName, NotifyType.StatusMessage);
                }
                ResetReadCancellationTokenSource();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs eventArgs)
        {
            IsNavigatedAway = true;
            CancelAllIoTasks();
        }

        public string Read(uint ReadBufferLength)
        {
            string readData = "";
            oneChar = "";
            ReadBytesCounter = 0;
            try
            {
                if (CommPortClass.serialPort != null)
                {
                    try
                    {
                        IsReadTaskPending = true;
                        DataReaderObject = new DataReader(CommPortClass.serialPort.InputStream);
                        readData = ReadAsync(ReadBufferLength, ReadCancellationTokenSource.Token);
                    }
                    catch (Exception exception)
                    {
                        MainPage.Current.NotifyUser(exception.Message.ToString(), NotifyType.ErrorMessage);
                        Debug.WriteLine(exception.Message.ToString());
                    }
                    finally
                    {
                        IsReadTaskPending = false;
                        if (ReadBytesCounter != 0)
                        {
                            DataReaderObject.DetachStream();
                            DataReaderObject = null;
                        }
                    }
                }
                else
                {
                    MainPage.Current.NotifyUser("Device is not connected", NotifyType.ErrorMessage);
                }
            }
            catch (Exception e)
            { }
            return readData;
        }

        public string ReadLine()
        {

            string readData = "";
            try
            {
                for (int i = 0; i < 40; i++)
                {
                    oneChar = Read(1);
                    //Task.Delay(100);
                    if (oneChar != "\r")
                    {
                        readData += oneChar;
                    }
                    else
                    {
                        i = 40;
                    }
                }
            }
            catch (Exception e)
            {
            }
            return readData;
        }

        private string ReadAsync(uint ReadBufferLength, CancellationToken cancellationToken)
        {
            string outputData = "";
            UInt32 bytesRead = 0;
            Task<UInt32> loadAsyncTask;
            try
            {
                lock (ReadCancelLock)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    DataReaderObject.InputStreamOptions = InputStreamOptions.ReadAhead;
                    loadAsyncTask = DataReaderObject.LoadAsync(ReadBufferLength).AsTask(cancellationToken);
                }
                Task t = Task.Run(async () => { bytesRead = await loadAsyncTask; });
                bool b = t.Wait(1000);
                if (b == false)
                {
                    //rootPage.NotifyUser("Read completed - " + bytesRead.ToString() + " bytes were read", NotifyType.StatusMessage);
                    Task t1 = Task.Run(() => { CancelReadTask(); });
                    ResetReadCancellationTokenSource();
                }
                if (bytesRead > 0)
                {
                    outputData = DataReaderObject.ReadString(bytesRead);
                    ReadBytesCounter += bytesRead;
                }
                //rootPage.NotifyUser("Read completed - " + bytesRead.ToString() + " bytes were read", NotifyType.StatusMessage);
            }
            catch (Exception e)
            {
            }
            return outputData;
        }


        //private async void WriteButton_Click(object sender, RoutedEventArgs e)
        public async void Write(string dataToWrite)
        {
            if (CommPortClass.serialPort != null)
            {
                try
                {
                    if (CommPortClass.serialPort != null)
                    {
                        DataWriteObject = new DataWriter(CommPortClass.serialPort.OutputStream);
                        await WriteAsync(dataToWrite);
                    }
                    else
                    {
                        MainPage.Current.NotifyUser("Device is not connected", NotifyType.ErrorMessage);
                    }
                }
                catch (OperationCanceledException /*exception*/)
                {
                    NotifyWriteTaskCanceled();
                }
                catch (Exception exception)
                {
                    MainPage.Current.NotifyUser(exception.Message.ToString(), NotifyType.ErrorMessage);
                    Debug.WriteLine(exception.Message.ToString());
                }
                finally
                {
                    if (DataWriteObject != null)
                    {
                        DataWriteObject.DetachStream();
                        DataWriteObject = null;
                    }
                }
            }
            else
            {
                MainPage.Current.NotifyUser("Device is not connected", NotifyType.ErrorMessage);
            }
        }

        private async Task WriteAsync(string dataToWrite)
        {
            Task<UInt32> storeAsyncTask;

            if (dataToWrite.Length != 0)
            {
                DataWriteObject.WriteString(dataToWrite);

                storeAsyncTask = DataWriteObject.StoreAsync().AsTask();
                UInt32 bytesWritten = await storeAsyncTask;
            }
            else
            {
                MainPage.Current.NotifyUser("No data to sent to port", NotifyType.ErrorMessage);
            }
        }

        private void CancelReadTask()
        {
            try
            {
                lock (ReadCancelLock)
                {
                    if (ReadCancellationTokenSource != null)
                    {
                        if (!ReadCancellationTokenSource.IsCancellationRequested)
                        {
                            ReadCancellationTokenSource.Cancel();

                            // Existing IO already has a local copy of the old cancellation token so this reset won't affect it
                            ResetReadCancellationTokenSource();
                        }
                    }
                }
            }
            catch (Exception e)
            { }
        }

        private void CancelAllIoTasks()
        {
            CancelReadTask();
        }

        private Boolean IsPerformingRead()
        {
            return (IsReadTaskPending);
        }

        private Boolean IsPerformingWrite()
        {
            return (IsWriteTaskPending);
        }

        private void ResetReadCancellationTokenSource()
        {
            ReadCancellationTokenSource = new CancellationTokenSource();
            ReadCancellationTokenSource.Token.Register(() => { });
        }

        private async void NotifyReadTaskCanceled()
        {
            try
            {
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    new DispatchedHandler(() =>
                    {
                        if (!IsNavigatedAway)
                        {
                            rootPage.NotifyUser("Read request has been cancelled", NotifyType.StatusMessage);
                        }
                    }));
            }
            catch(Exception e)
            { }
        }

        private async void NotifyWriteTaskCanceled()
        {
            try
            {
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    new DispatchedHandler(() =>
                    {
                        if (!IsNavigatedAway)
                        {
                            rootPage.NotifyUser("Write request has been cancelled", NotifyType.StatusMessage);
                        }
                    }));
            }
            catch(Exception e)
            { }
        }
        
    }
}
