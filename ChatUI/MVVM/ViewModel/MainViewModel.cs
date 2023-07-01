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

				// run event
				MainWindow.OnMessageAdded(new MessageEventArgs(Message));

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


		public void AddOtherMessage(string mainMessage, string subMessage, string userName)
		{
			Messages.Add(new MessageModel
			{
				Username = userName,
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

		public void AddLoadingSpinner()
		{
			Messages.Add(new MessageModel
			{
				IsLoadingSpinner = true
			});
			ScrollToBottom();
		}

		public void DeleteLoadingSpinner()
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
