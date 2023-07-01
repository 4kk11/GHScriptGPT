using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Windows;
using System.Collections.Generic;
using Grasshopper.GUI;
using Grasshopper.Kernel.Special;
using Grasshopper.GUI.Script;
using Rhino;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Interop;
using ChatUI;
using ChatGPTConnection;
using GHScriptGPT.Prompts;

namespace GHScriptGPT
{
	public class GHScriptGPT : GH_AssemblyPriority
	{
		public override GH_LoadingInstruction PriorityLoad()
		{
			Instances.CanvasCreated += Instances_CanvasCreated;
			return GH_LoadingInstruction.Proceed;
		}

		private void Instances_CanvasCreated(Grasshopper.GUI.Canvas.GH_Canvas canvas)
		{
			Instances.CanvasCreated -= Instances_CanvasCreated;
			RegisterNewMenuItems();
		}

		private void RegisterNewMenuItems()
		{
			GH_DocumentEditor docEditor = Instances.DocumentEditor;

			docEditor.MainMenuStrip.SuspendLayout();

			ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem();
			toolStripMenuItem.Name = "GHScriptGPT";
			toolStripMenuItem.Text = "GHScriptGPT";

			toolStripMenuItem.Click += (sender, e) =>
			{
				//チャットGUIを呼び出す
				OpenChatUI();
				//System.Windows.MessageBox.Show("Hello World!");
			};

			docEditor.MainMenuStrip.ResumeLayout(false);
			docEditor.MainMenuStrip.PerformLayout();

			docEditor.MainMenuStrip.Items.Add(toolStripMenuItem);
		}

		private MainWindow OpenChatUI()
		{
			if (IsWindowOpen<ChatUI.MainWindow>()) return null;
			MainWindow window = new ChatUI.MainWindow();

			window.MessageAdded -= MessageAdded;
			window.MessageAdded += MessageAdded;

			//Rhinoのウィンドウを親に設定
			var rhinoHandle = RhinoApp.MainWindowHandle();
			var helper = new WindowInteropHelper(window);
			helper.Owner = rhinoHandle;

			window.Show();
			return window;
		}

		private bool IsWindowOpen<T>() where T : Window
		{
			foreach (Window window in System.Windows.Application.Current.Windows)
			{
				if (window is T) return true;
			}
			return false;
		}

		private void ResponseRecieved(ChatGPTResponseModel response)
		{
			//todo: RunScript(...){}を取り除く
			Dictionary<string, string> jsonDict = response.GetJsonDict();
			string conversation = jsonDict["conversation"];
			string code = jsonDict["code"];

			GH_ScriptEditor editor = GetCurrentEditor();
			if(editor == null) return;

			GH_CodeBlocks codeBlocks = editor.GetSourceCode();
			codeBlocks[3].AddLine($"//{conversation}");
			var lines = code.Split('\n');
			codeBlocks[3].AddLines(lines);
			editor.SetSourceCode(codeBlocks);


		}

		private string GetBaseCode()
		{
			GH_ScriptEditor editor = GetCurrentEditor();
			if (editor == null) return null;
			GH_CodeBlocks codeBlocks = editor.GetSourceCode();
			var lines = codeBlocks[2].Lines;
			lines = lines.Skip(lines.Count() - 2);
			lines = lines.Concat(codeBlocks[3].Lines);
			lines = lines.Append("}");
			lines = lines.Select(text => text.TrimStart());
			string baseCode = String.Join("\n", lines);
			return baseCode;
		}

		private GH_ScriptEditor GetCurrentEditor()
		{
			var forms = System.Windows.Forms.Application.OpenForms;
			foreach (var form in forms)
			{
				if (form.GetType().Name != "GH_ScriptEditor") continue;
				GH_ScriptEditor editor = form as GH_ScriptEditor;
				return editor;
			}
			return null;
		}

		private string CreateUserPrompt(string requestMessage, string baseCode)
		{
			PromptTemplate template = PromptTemplate.FromFile("user.txt");
			Dictionary<string, string> replacements= new Dictionary<string, string> 
			{
				{"request", requestMessage},
				{"base", baseCode}
			};
			return template.FormatPrompt(replacements);
		}

		private string CreateSystemPrompt()
		{
			PromptTemplate template = PromptTemplate.FromFile("system_ja.txt");
			Dictionary<string, string> replacements = new Dictionary<string, string>();
			return template.FormatPrompt(replacements);
		}

		private async void MessageAdded(object sender, MessageEventArgs e)
		{
			//todo: Editorが一つだけ開いているかどうかを確かめる

			MainWindow window = (MainWindow)sender;
			string requestMessage = e.Message;
			string baseCode = GetBaseCode();

			string userPrompt = CreateUserPrompt(requestMessage, baseCode);

			window.AddLoadingSpinner();
			try
			{
				//Get API key from settings file
				Settings settings = Settings.LoadSettings();
				if (settings == null)
				{
					System.Windows.MessageBox.Show("Setting file is not found. Please set that from options.");
					return;
				}
				string apiKey = settings.APIKey;
				string organizationID = settings.OrganizationID;
				string modelName = settings.ModelName;
				string systemMessage = CreateSystemPrompt();

				if (apiKey == "")
				{
					System.Windows.MessageBox.Show("API key not found. Please set from the options.");
					return;
				}
				if (modelName == "")
				{
					System.Windows.MessageBox.Show("ModelName is not setted. Please set from the options.");
					return;
				}

				// API request
				var chatGPTConnector = new ChatGPTConnector(apiKey, organizationID, modelName, systemMessage);
				var response = await chatGPTConnector.RequestAsync(userPrompt);

				if (!response.isSuccess)
				{

					window.AddOtherMessage("API request failed. Settings may be wrong.", null, "ChatGPT");
					return;
				}

				// Apply to editor
				ResponseRecieved(response);

				//Add reply to chat
				string conversationText = response.GetConversation();
				string fullText = response.GetMessage();
				window.AddOtherMessage(conversationText, fullText, "ChatGPT");
			}
			finally
			{
				window.DeleteLoadingSpinner();
			}
			
		}

	}

	public class GHScriptGPTInfo : GH_AssemblyInfo
	{
		public override string Name => "GHScriptGPT";
		public override Bitmap Icon => null;
		public override string Description => "";
		public override Guid Id => new Guid("e38cd998-8acf-4b22-acea-2a4c2292eefb");
		public override string AuthorName => "";
		public override string AuthorContact => "";
	}
}