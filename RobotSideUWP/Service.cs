﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:2.0.50727.3603
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.ServiceModel;


[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(Namespace = "http://boteyes.ru/", ConfigurationName = "IService1")]
public interface IService1
	{

    [System.ServiceModel.OperationContractAttribute(Action = "http://boteyes.ru/IService1/DoWork", ReplyAction = "http://boteyes.ru/IService1/DoWorkResponse")]
	string[] DoWork(string[] serialNumber);

    [System.ServiceModel.OperationContractAttribute(Action = "http://boteyes.ru/IService1/CreateKey", ReplyAction = "http://boteyes.ru/IService1/CreateKeyResponse")]
    string CreateKey(string key);
    //string[] DoWork(string serialNumber);
	}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public interface IService1Channel : IService1, System.ServiceModel.IClientChannel
	{
	}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "3.0.0.0")]
public partial class Service1Client : ClientBase<IService1>, IService1
	{
	public Service1Client()
		{
		
		}

	public Service1Client(string endpointConfigurationName) :
		base(endpointConfigurationName)
		{
		}

	public Service1Client(string endpointConfigurationName, string remoteAddress) :
		base(endpointConfigurationName, remoteAddress)
		{
		}

	public Service1Client(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
		base(endpointConfigurationName, remoteAddress)
		{

		}

	public Service1Client(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
		base(binding, remoteAddress)
		{
		}

	public string[] DoWork(string[] serialNumber)
		{
		string[] result = base.Channel.DoWork(serialNumber);
		return result;
		}

    public string CreateKey(string key)
        {
        string keyIs = base.Channel.CreateKey(key);
        return keyIs;
        }
	}
