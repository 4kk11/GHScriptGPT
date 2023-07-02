# GHScriptGPT
#### This plugin utilizes the OpenAI API functionality to generate source code for Grasshopper's C# scripting components.
![image](https://github.com/4kk11/GHScriptGPT/assets/61794994/162de6bf-1f4a-4119-95fe-49fa4655d1f3)

## Features


## Installation
1. Download the plugin files from the Releases section of this repository.
2. Put it in the Grasshopper/Libraries folder.

## Usage
#### First, configure the API key and other settings from the gear button.   
#### After setting the API key, make sure that Settings.xml is generated in the plugin folder.   

![image2](https://github.com/4kk11/GHScriptGPT/assets/61794994/9af14fe2-5a3f-4703-ac92-e8f88e29f7f6)

Each setting item is described below.  

* **APIKey**: Put the API key of OpenAI API. If you do not have an OpenAI API account, please sign up on [this page](https://openai.com/product).   

* **OrganizationID**: Specify the OrganizationID of OpenAI API if necessary. If you do not enter anything, the default one will be used.  

* **ModelName**: Specify the model to be used. The names of available models are on [this page](https://platform.openai.com/account/rate-limits).   

* **PromptLanguage**: Set the language to be used in the Prompt. This will change the language of the response from ChatGPT.   
   
<br>

#### Be sure to use a single C# script editor open. 
If you do not have an editor open or have more than one editor open, you will receive a warning.

![image3](https://github.com/4kk11/GHScriptGPT/assets/61794994/37e6ebb7-270a-412c-91b6-89bc1c8e544c)


