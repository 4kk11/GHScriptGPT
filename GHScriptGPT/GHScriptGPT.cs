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
using static System.Net.Mime.MediaTypeNames;
using System.Text;
using GHScriptGPT.Scripts;

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
				//call chat GUI
				OpenChatUI();
			};

			docEditor.MainMenuStrip.ResumeLayout(false);
			docEditor.MainMenuStrip.PerformLayout();

			docEditor.MainMenuStrip.Items.Add(toolStripMenuItem);
		}

		private MainWindow OpenChatUI()
		{
			// Returns null if it is already open
			if (IsWindowOpen<ChatUI.MainWindow>()) return null;

			MainWindow window = new ChatUI.MainWindow();

			// Event fired when user's message is added
			window.MessageAdded -= MessageAdded;
			window.MessageAdded += MessageAdded;

			// Set Rhino window as parent
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

		private async void MessageAdded(object sender, MessageEventArgs e)
		{
			//todo: Editor‚ªˆê‚Â‚¾‚¯ŠJ‚¢‚Ä‚¢‚é‚©‚Ç‚¤‚©‚ðŠm‚©‚ß‚é
			CurrentEditor editor = CurrentEditor.GetCurrentEditor();
			if(editor == null) return;

			MainWindow window = (MainWindow)sender;
			string requestMessage = e.Message;
			string baseCode = editor.GetCode_RunScript().CodeText;

			// Addition error info
			IEnumerable<string> errors = editor.GetErrors();
			if (errors.Any())
			{
				string errorsText = String.Join("\n", errors);
				requestMessage = requestMessage + "\n" + errorsText;
			}

			string userPrompt = PromptTemplate.CreateUserPrompt(requestMessage, baseCode);
			string systemPrompt = PromptTemplate.CreateSystemPrompt();

			window.AddLoadingSpinner();
			try
			{
				// Get API key from settings file
				Settings settings = Settings.LoadSettings();
				if (settings == null)
				{
					System.Windows.MessageBox.Show("Setting file is not found. Please set that from options.");
					return;
				}
				string apiKey = settings.APIKey;
				string organizationID = settings.OrganizationID;
				string modelName = settings.ModelName;

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
				var chatGPTConnector = new ChatGPTConnector(apiKey, organizationID, modelName, systemPrompt);
				var response = await chatGPTConnector.RequestAsync(userPrompt);

				if (!response.isSuccess)
				{
					window.AddOtherMessage("API request failed. Settings may be wrong.", null, "ChatGPT");
					return;
				}

				// Get json dictionary from response
				Dictionary<string, string> jsonDict = response.GetJsonDict();
				string conversationText = jsonDict["conversation"];
				if (jsonDict["result"] == "Success")
				{			
					string codeText = jsonDict["code"];
					// Apply response code to editor
					ApplyCode(codeText, editor);
				}

				// Add reply to chat
				string fullText = response.GetMessage();
				window.AddOtherMessage(conversationText, fullText, "ChatGPT");
			}
			finally
			{
				window.DeleteLoadingSpinner();
			}
			
		}

		private void ApplyCode(string codeText, CurrentEditor editor)
		{
			SouceCode souceCode = new SouceCode(codeText);
			SouceCode extractedCode = souceCode.ExtractFunctionCode("private void RunScript");
			editor.SetCode_RunScript(extractedCode);
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