﻿using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Translation.Api;
using Translation.WebApi;
using Translation.WebApi.KinsoftApi;
using Translation.WebApi.YouDaoApi;
using TranslationTool.Annotations;
using TranslationTool.Helper;

namespace TranslationTool.ViewModels
{
    public class SearchWordViewModel : ViewModelBase
    {
        public SearchWordViewModel()
        {
            SearchCommand = new DelegateCommand(Search_OnExecute);
            SpeekCommand = new DelegateCommand<string>(Speek_OnExecute);
        }

        #region 发音

        public ICommand SpeekCommand { get; }
        private Mp3Player _mp3Player = null;

        private void Speek_OnExecute(string audioPath)
        {
            if (File.Exists(audioPath))
            {
                if (_mp3Player == null)
                {
                    _mp3Player = new Mp3Player();
                }
                else
                {
                    _mp3Player.Pause();

                }
                _mp3Player.FilePath = audioPath;
                _mp3Player.Play();
            }
        }

        #endregion

        #region 搜索

        private string _searchingText;
        public string SearchingText
        {
            get => _searchingText;
            set
            {
                _searchingText = value;
                OnPropertyChanged();
            }
        }

        public ICommand SearchCommand { get; }
        private async void Search_OnExecute()
        {
            var searchingText = SearchingText ?? string.Empty;
            await SearchWord(searchingText);
        }

        #region 搜索单词

        /// <summary>
        /// 搜索单词
        /// </summary>
        /// <param name="searchingText"></param>
        /// <returns></returns>
        private async Task SearchWord(string searchingText)
        {
            var wordData = new EnglishWordTranslationData();
            if (!string.IsNullOrWhiteSpace(searchingText))
            {
                switch (SelectedApiType)
                {
                    case "有道":
                        {
                            wordData = await YouDaoUnOfficialWordApiService.GetWordsAsync(searchingText);
                        }
                        break;
                    case "金山":
                        {
                            wordData = await KinsoftUnOfficialApiService.GetWordsAsync(searchingText);
                        }
                        break;
                    default:
                        {
                            var wordDataYoudao = await YouDaoUnOfficialWordApiService.GetWordsAsync(searchingText);
                            var wordDataKinsoft = await KinsoftUnOfficialApiService.GetWordsAsync(searchingText);
                            wordData.Word = wordDataKinsoft.Word;
                            wordData.UsPronounce = wordDataKinsoft.UsPronounce;
                            wordData.UkPronounce = wordDataKinsoft.UkPronounce;
                            wordData.DetailJson = wordDataYoudao.DetailJson + "\r\n" + wordDataKinsoft.DetailJson;

                            wordData.Translations = wordDataKinsoft.Translations;
                            wordData.Phrases = wordDataYoudao.Phrases;
                            wordData.Sentences = wordDataKinsoft.Sentences;
                            wordData.Synonyms = wordDataYoudao.Synonyms.Count > 0 ? wordDataYoudao.Synonyms : wordDataKinsoft.Synonyms;
                            wordData.Cognates = wordDataYoudao.Cognates.Count > 0 ? wordDataYoudao.Cognates : wordDataKinsoft.Cognates;
                        }
                        break;
                }
            }

            SetSearchedWordData(wordData);
        }


        private void SetSearchedWordData(EnglishWordTranslationData wordData)
        {
            var result = wordData;
            CurrentWord = result.Word;
            SearchResultDetail = result.DetailJson;
            SetTranslations(wordData.Translations);
            UsPronounce = PronounceModel.ConvertFrom(result.UsPronounce);
            UkPronounce = PronounceModel.ConvertFrom(result.UkPronounce);
            SetPhrases(result.Phrases);
            SetSynonyms(result.Synonyms);
            SetCognates(result.Cognates);
            SetSentences(result.Sentences);

            //默认英式发音
            if (UkPronounce != null)
            {
                Speek_OnExecute(UkPronounce.PronounceUri);
            }
        }

        private void SetCognates(List<CognateInfo> resultCognates)
        {
            var cognateModels = new List<CognateModel>();
            int index = 1;
            foreach (var resultCognate in resultCognates)
            {
                var cognateModel = new CognateModel()
                {
                    Cognate = $"{index++}. {resultCognate.Cognate}",
                    Translation = $"{resultCognate.WordType} {resultCognate.Translation}",
                };
                cognateModels.Add(cognateModel);
            }
            Cognates = cognateModels;
        }

        private void SetSynonyms(List<SynonymInfo> resultSynonyms)
        {
            var list = new List<SynonymModel>();
            int index = 1;
            foreach (var resultSynonym in resultSynonyms)
            {
                var model = new SynonymModel()
                {
                    Translation = $"{index++}. {resultSynonym.Translation}",
                    Synonyms = string.Join(";", resultSynonym.Synonyms),
                };
                list.Add(model);
            }
            Synonyms = list;
        }

        private void SetPhrases(List<PhraseInfo> resultPhrases)
        {
            var list = new List<PhraseModel>();
            int index = 1;
            foreach (var sentenceInfo in resultPhrases)
            {
                var phraseModel = new PhraseModel()
                {
                    Phrase = $"{index++}. {sentenceInfo.Phrase}",
                    PhraseTranslation = sentenceInfo.PhraseTranslation,
                };

                list.Add(phraseModel);
            }
            Phrases = list;
        }

        private void SetTranslations(List<SematicInfo> wordDataTranslations)
        {
            var translations = new List<string>();
            int index = 1;
            foreach (var wordDataTranslation in wordDataTranslations)
            {

                translations.Add($"{index++}.{ wordDataTranslation.WordType}{ wordDataTranslation.Translation}");
            }
            Translations = translations;
        }
        private void SetSentences(List<SentenceInfo> sentenceInfos)
        {
            var sentences = new List<SentenceModel>();
            int index = 1;
            foreach (var sentenceInfo in sentenceInfos)
            {
                var sentenceModel = new SentenceModel()
                {
                    EnglishSentence = $"{index++}. {sentenceInfo.Sentence}",
                    ChineseSentence = sentenceInfo.Translation,
                };
                if (!string.IsNullOrWhiteSpace(sentenceInfo.EnglishSentenceUri))
                {
                    var downloadSuccess = WebResourceDownloadHelper.Download(sentenceInfo.EnglishSentenceUri, out string downloadPath);
                    if (downloadSuccess)
                    {
                        sentenceModel.EnglishSentenceUri = downloadPath;
                    }
                }
                sentences.Add(sentenceModel);
            }
            Sentences = sentences;
        }

        //官方查询API
        //private async void Search_OnExecute()
        //{
        //    var result = await YouDaoOfficialApiService.GetWordsAsync(SearchingText);

        //    Explaniation = result.YouDaoTranslation.BasicTranslation.Phonetic + "\r\n" +
        //                   string.Join("\r\n", result.YouDaoTranslation.BasicTranslation.Explains);
        //    SearchResultDetail = result.ResultDetail;
        //}

        #endregion

        #endregion

        #region 搜索结果

        private string _currentWord = string.Empty;
        public string CurrentWord
        {
            get => _currentWord;
            set
            {
                _currentWord = value;
                OnPropertyChanged();
            }
        }        /// <summary>
                 /// 英式发音
                 /// </summary>
        private PronounceModel _ukPronounce;
        public PronounceModel UkPronounce
        {
            get => _ukPronounce;
            set
            {
                _ukPronounce = value;
                OnPropertyChanged();
            }
        }
        private PronounceModel _usPronounce;
        /// <summary>
        /// 美式发音
        /// </summary>
        public PronounceModel UsPronounce
        {
            get => _usPronounce;
            set
            {
                _usPronounce = value;
                OnPropertyChanged();
            }
        }

        private List<SentenceModel> _sentences;
        public List<SentenceModel> Sentences
        {
            get => _sentences;
            set
            {
                _sentences = value;
                OnPropertyChanged();
            }
        }

        private List<PhraseModel> _phrases;
        public List<PhraseModel> Phrases
        {
            get => _phrases;
            set
            {
                _phrases = value;
                OnPropertyChanged();
            }
        }
        private List<SynonymModel> _synonyms;
        public List<SynonymModel> Synonyms
        {
            get => _synonyms;
            set
            {
                _synonyms = value;
                OnPropertyChanged();
            }
        }
        private List<CognateModel> _cognates;
        public List<CognateModel> Cognates
        {
            get => _cognates;
            set
            {
                _cognates = value;
                OnPropertyChanged();
            }
        }
        private string _searchResultDetail;
        public string SearchResultDetail
        {
            get => _searchResultDetail;
            set
            {
                _searchResultDetail = value;
                OnPropertyChanged();
            }
        }

        private List<string> _translations;
        public List<string> Translations
        {
            get => _translations;
            set
            {
                _translations = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region API类型

        public List<string> ApiTypeList { get; set; } = new List<string>()
        {
            "默认","有道","金山"
        };

        private string _selectedApiType = string.Empty;

        public string SelectedApiType
        {
            get
            {
                if (string.IsNullOrEmpty(_selectedApiType))
                {
                    _selectedApiType = ApiTypeList[0];
                }

                return _selectedApiType;
            }
            set
            {
                _selectedApiType = value;
                OnPropertyChanged();
            }
        }



        #endregion

    }

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
