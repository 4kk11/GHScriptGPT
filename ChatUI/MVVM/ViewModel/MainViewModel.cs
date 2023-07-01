﻿using ChatGPTConnection;
using ChatUI.Core;
using ChatUI.MVVM.Model;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Net.Mail;

namespace ChatUI.MVVM.ViewModel
{
	internal class MainViewModel : ObservableObject
	{
		public ObservableCollection<MessageModel> Messages { get; set; }
		private MainWindow MainWindow { get; set; }
		public RelayCommand SendCommand { get; set; }

		private string _message = "";
		public string Message
		{
			get { return _message; }
			set
			{
				_message = value;
				OnPropertyChanged();
			}
		}

		private string CatIconPath => Path.Combine(MainWindow.DllDirectory, "Icons/icon.jpeg");
		
		public MainViewModel()
		{
			Messages = new ObservableCollection<MessageModel>();

			//ビュー(?)を取得
			var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x is MainWindow);
			MainWindow = (MainWindow)window;

			//キーを押したらメッセージが追加されるコマンド
			SendCommand = new RelayCommand(o =>
			{
				if (Message == "") return;

				//自分のメッセージを追加
				AddMyMessages(Message);

				//ChatGPTにメッセージをおくり、返信をメッセージに追加
				SendToChatGPT(Message);

				//メッセージボックスを空にする
				Message = "";
			});
			//Test_Message();
		}

		private void AddMyMessages(string message)
		{
			Messages.Add(new MessageModel
			{
				Username = "You",
				UsernameColor = "White",
				Time = DateTime.Now,
				MainMessage = message,
				IsMyMessage = true
			});

			ScrollToBottom();
		}

		//TODO: 多責務になっているので分割したい
		private ChatGPTConnector _chatGPTConnector = null;
		private async void SendToChatGPT(string message)
		{
			//LoadingSpinnerを表示
			AddLoadingSpinner();

			//APIキーをセッティングファイルから取得
			Settings settings = Settings.LoadSettings();
			if (settings == null)
			{
				MessageBox.Show("Setting file is not found. Please set that from options.");
				DeleteLoadingSpinner();
				return;
			}
			string apiKey = settings.APIKey;
			string organizationID = settings.OrganizationID;
			string modelName = settings.ModelName;
			string systemMessage = settings.SystemMessage;

			// Check
			if (apiKey == "")
			{
				MessageBox.Show("API key not found. Please set from the options.");
				DeleteLoadingSpinner();
				return;
			}
			if (modelName == "")
			{
				MessageBox.Show("ModelName is not setted. Please set from the options.");
				DeleteLoadingSpinner();
				return;
			}
			//ChatGPTにメッセージを送る
			_chatGPTConnector = new ChatGPTConnector(apiKey, organizationID, modelName, systemMessage);
			var response = await _chatGPTConnector.RequestAsync(message);

			//LoadingSpinnerを削除
			DeleteLoadingSpinner();

			if (!response.isSuccess)
			{
				AddChatGPTMessage("API request failed. Settings may be wrong.", null);
				return;
			}

			//返信をチャット欄に追加
			string conversationText = response.GetConversation();
			string fullText = response.GetMessage();
			AddChatGPTMessage(conversationText, fullText);

			//イベントを実行
			MainWindow.OnResponseReceived(new ChatGPTResponseEventArgs(response));
			
		}

		

		private void AddChatGPTMessage(string mainMessage, string subMessage)
		{
			Messages.Add(new MessageModel
			{
				Username = "ChatGPT",
				UsernameColor = "#738CBA",
				ImageSource = CatIconPath,
				Time = DateTime.Now,
				MainMessage = mainMessage,
				SubMessage = subMessage,
				UseSubMessage = MainWindow.IsDebagMode,
				IsMyMessage = false
			});

			ScrollToBottom();
		}

		private void ScrollToBottom()
		{
			int lastIndex = MainWindow.ChatView.Items.Count - 1;
			var item = MainWindow.ChatView.Items[lastIndex];
			MainWindow.ChatView.ScrollIntoView(item);
		}

		private void AddLoadingSpinner()
		{
			Messages.Add(new MessageModel
			{
				IsLoadingSpinner = true
			});
			ScrollToBottom();
		}

		private void DeleteLoadingSpinner()
		{
			for (int i = 0; i < Messages.Count; i++)
			{
				var item = Messages[i];
				if (item.IsLoadingSpinner)
				{
					Messages.Remove(item);
				}
			}
		}

		private void Test_Message()
		{
			Messages.Add(new MessageModel
			{
				Username = "ChatGPT",
				UsernameColor = "#738CBA",
				ImageSource = CatIconPath,
				Time = DateTime.Now,
				MainMessage = "aaaaaaa",
				UseSubMessage = MainWindow.IsDebagMode,
				IsMyMessage = false
			});
		}
	}
}
