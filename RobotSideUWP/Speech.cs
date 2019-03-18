using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.Resources.Core;
using Windows.Data.Xml.Dom;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace RobotSideUWP
{
    public sealed partial class MainPage : Page
    {
        private SpeechSynthesizer synthesizer;
        private bool IsSSML = true;
        private ResourceContext speechContext;
        private ResourceMap speechResourceMap;
        public MainPage rootPage = Current;
        public MediaElement mediaElement;

        public void InitializeSpeech()
        {
            CommonFunctions.Paths();
            //CommonStruct.textToRead = CommonFunctions.ReadFromSSMLSource();
            synthesizer = new SpeechSynthesizer();
            speechContext = ResourceContext.GetForCurrentView();
            speechContext.Languages = new string[] { SpeechSynthesizer.DefaultVoice.Language };
            speechResourceMap = ResourceManager.Current.MainResourceMap.GetSubtree("LocalizationTTSResources");
            UpdateSSMLText();
            ListboxVoiceChooser_Initialize();
            mediaElement = media;
        }

        public async void SpeechStart()
        {
            IsSSML = true;
            string textToRead = CommonFunctions.ReadFromSSMLSource();
            CommonStruct.textToRead = textToRead;
            VoiceInformation currentVoice = this.synthesizer.Voice;
            SpeechSynthesisStream stream = await this.synthesizer.SynthesizeSsmlToStreamAsync(textToRead);
            mediaElement.SetSource(stream, stream.ContentType);
            mediaElement.Play();
        }

        public async void SpeechStart(string textToRead)
        {
            IsSSML = false;
            CommonStruct.textToRead = textToRead;
            VoiceInformation currentVoice = synthesizer.Voice;
            SpeechSynthesisStream stream = await synthesizer.SynthesizeTextToStreamAsync(textToRead);
            mediaElement.SetSource(stream, stream.ContentType);
            //mediaElement.Play();
        }


        public void SpeechStop()
        {
            media.Stop();
        }

        private async void SaveToFile()
        {
            string text = CommonStruct.textToRead;
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.DefaultFileExtension = ".wav";
            savePicker.FileTypeChoices.Add("Audio file", new List<string>() { ".wav" });
            SpeechSynthesisStream synthesisStream;
            try
            {
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    if (IsSSML == true)
                    {
                        synthesisStream = await this.synthesizer.SynthesizeSsmlToStreamAsync(text);
                    }
                    else
                    {
                        synthesisStream = await this.synthesizer.SynthesizeTextToStreamAsync(text);
                    }

                    if (synthesisStream == null)
                    {
                        return;
                    }
                    Windows.Storage.Streams.Buffer buffer = new Windows.Storage.Streams.Buffer(4096);
                    IRandomAccessStream writeStream = (IRandomAccessStream)await file.OpenAsync(FileAccessMode.ReadWrite);
                    IOutputStream outputStream = writeStream.GetOutputStreamAt(0);
                    DataWriter dataWriter = new DataWriter(outputStream);

                    while (synthesisStream.Position < synthesisStream.Size)
                    {
                        await synthesisStream.ReadAsync(buffer, 4096, InputStreamOptions.None);
                        dataWriter.WriteBuffer(buffer);
                    }
                    dataWriter.StoreAsync().AsTask().Wait();
                    outputStream.FlushAsync().AsTask().Wait();
                    outputStream.Dispose();
                }
            }
            catch (Exception e2)
            {
                synthesisStream = null;
                rootPage.NotifyUserFromOtherThread("SaveToFile" + e2.Message + " Файл, наверное, занят.", NotifyType.StatusMessage);
            }
        }

        private void UpdateSSMLText()
        {
            try
            {
                string text = CommonStruct.textToRead;
                string language = this.synthesizer.Voice.Language;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(text);

                var LangAttribute = doc.DocumentElement.GetAttributeNode("xml:lang");
                LangAttribute.InnerText = language;

                CommonStruct.textToRead = doc.GetXml();
            }
            catch (Exception )
            {
            }
        }

        private void ListboxVoiceChooser_Initialize()
        {
            var voices = SpeechSynthesizer.AllVoices;
            VoiceInformation currentVoice = synthesizer.Voice;
            foreach (VoiceInformation voice in voices.OrderBy(p => p.Language))
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Name = voice.DisplayName;
                item.Tag = voice;
                item.Content = voice.DisplayName + " (Language: " + voice.Language + ")";
                listboxVoiceChooser.Items.Add(item);
                if (currentVoice.Id == voice.Id)
                {
                    item.IsSelected = true;
                    listboxVoiceChooser.SelectedItem = item;
                }
            }
        }

        private void ListboxVoiceChooser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem item = (ComboBoxItem)(listboxVoiceChooser.SelectedItem);
            VoiceInformation voice = (VoiceInformation)(item.Tag);
            synthesizer.Voice = voice;
            speechContext.Languages = new string[] { voice.Language };
        }

    }
}
