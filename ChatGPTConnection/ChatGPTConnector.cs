﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ChatGPTConnection
{
	public class ChatGPTConnector
	{

		private readonly string _systemMessage;

		private readonly string _apiKey;
		private readonly string _organizationID;
		private readonly string _modelName;

		//会話履歴を保持するリスト
		//今回は送信の度にこのクラスをインスタンス化するので過去の会話は保持されない
		private readonly List<ChatGPTMessageModel> _messageList = new List<ChatGPTMessageModel>();

		public ChatGPTConnector(string apiKey, string organizationID, string modelName, string systemMessage)
		{
			_apiKey = apiKey;
			_organizationID = organizationID;
			_modelName = modelName;
			_systemMessage = systemMessage;
			_messageList.Add(new ChatGPTMessageModel() { role = "system", content = _systemMessage });
		}

		public async Task<ChatGPTResponseModel> RequestAsync(string userMessage)
		{
			//文章生成AIのAPIのエンドポイントを設定
			var apiUrl = "https://api.openai.com/v1/chat/completions";
			_messageList.Add(new ChatGPTMessageModel { role = "user", content = userMessage });

			//OpenAIのAPIリクエストに必要なヘッダー情報を設定
			var headers = new Dictionary<string, string>
			{
				{"Authorization", "Bearer " + _apiKey},
				{"OpenAI-Organization", _organizationID},
			};

			//文章生成で利用するモデルやトークン上限、プロンプトをオプションに設定
			var options = new ChatGPTCompletionRequestModel()
			{
				model = _modelName,
				messages = _messageList,
				temperature = 0.0
			};

			var jsonOptions = JsonConvert.SerializeObject(options);

			var httpClient = new HttpClient();

			foreach (var header in headers)
			{
				httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
			}

			var response = await httpClient.PostAsync(apiUrl, new StringContent(jsonOptions, Encoding.UTF8, "application/json"));

			if (!response.IsSuccessStatusCode)
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				return new ChatGPTResponseModel() {isSuccess = false , errorMessage = errorContent};	
			}
			else
			{
				var content = await response.Content.ReadAsStringAsync();
				var responseObject = JsonConvert.DeserializeObject<ChatGPTResponseModel>(content);
				responseObject.isSuccess = true;
				_messageList.Add(new ChatGPTMessageModel { role = "assistant", content = responseObject.GetMessage() });
				return responseObject;
			}
		}



	}


	public class ChatGPTMessageModel
	{
		public string role;
		public string content;
	}

	//ChatGPT APIにRequestを送るためのJSON用クラス
	public class ChatGPTCompletionRequestModel
	{
		public string model;
		public List<ChatGPTMessageModel> messages;
		public double temperature;
	}

	//ChatGPT APIからのResponseを受け取るためのクラス
	public class ChatGPTResponseModel
	{
		public bool isSuccess;
		public string id;
		public string @object;
		public int created;
		public Choice[] choices;
		public Usage usage;
		public string errorMessage;

		public class Choice
		{
			public int index;
			public ChatGPTMessageModel message;
			public string finish_reason;
		}

		public class Usage
		{
			public int prompt_tokens;
			public int completion_tokens;
			public int total_tokens;
		}

		public string GetMessage()
		{
			return this.choices[0].message.content;
		}


		public Dictionary<string, string> GetJsonDict()
		{
			string messageContent = GetMessage();
			try
			{
				var jsonDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(messageContent);
				if (!jsonDict.ContainsKey("conversation")) throw new Exception("conversation is not found");
				jsonDict["result"] = "Success";
				return jsonDict;
			}
			catch (Exception e)
			{
				string exeptionMessage = e.Message;
				var jsonDict = new Dictionary<string, string>() { };
				jsonDict["conversation"] = exeptionMessage;
				jsonDict["result"] = "Failure";
				return jsonDict;
			}
			
			
		}
	
	}
}
