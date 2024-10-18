using ProSimSDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.ComponentModel.DataAnnotations;
using CefSharp.DevTools.CSS;

namespace Prosim2GSX
{
    public class ProsimInterface
    {
        // Our main ProSim connection
        //private readonly ProSimConnect _connection = new ProSimConnect();
        //private readonly Dictionary<string, DataRefTableItem> _dataRefs = new Dictionary<string, DataRefTableItem>();

        protected ServiceModel Model;
        protected ProSimConnect Connection;

        public ProsimInterface(ServiceModel model, ProSimConnect _connection) 
        {
            Model = model;
            Connection = _connection;

            // Register to receive connect and disconnect events
            //_connection.onConnect += Connection_onConnect;
            //_connection.onDisconnect += Connection_onDisconnect;
        }

        public void ConnectProsimSDK()
        {
            try
            {
                Logger.Log(LogLevel.Debug, "ProsimInterface:ConnectProsimSDK", $"Attempting to connect to Prosim Server: {Model.ProsimHostname}");
                Connection.Connect(Model.ProsimHostname);

            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ConnectProsimSDK", $"Error connecting to ProSim System: {ex.Message}");

            }
        }

        public bool IsProsimReady()
        {
            if (Connection.isConnected)
            {
                Logger.Log(LogLevel.Debug, "ProsimInterface:IsProsimReady", $"Connection to Prosim server established populating dataref table");
                //ParseSupportedDatarefs();
                return true;
            }
            else
            {
                return false;
            }
        }

        public dynamic ReadDataRef(string _dataRef)
        {
            //Logger.Log(LogLevel.Debug, "ProsimInterface:ReadDataRef", $"Dataref {_dataRef} - typeof {Connection.ReadDataRef(_dataRef).GetType()}");
            try
            {
                return Connection.ReadDataRef(_dataRef);
            }
            catch (Exception ex) 
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ReadDataRef", $"There was an error setting {_dataRef} - exception {ex.ToString()}");
                return null;
            }

//            return Connection.ReadDataRef(_dataRef);
        }

        public void SetProsimVariable(string _dataRef, object value)
        {
            DataRef dataRef = new DataRef(_dataRef, 100, Connection);
            try
            {
                dataRef.value = value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:SetProsimSetVariable", $"There was an error setting {_dataRef} value {value} - exception {ex.ToString()}");
            }
        }

        public object GetProsimVariable(string _dataRef)
        {
            Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Attempting to get {_dataRef}");
            //Connection.ReadDataRef( _dataRef );
            DataRef dataRef = new DataRef(_dataRef, 100, Connection);
            Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Dataref {(string)dataRef.value}");
            return dataRef.value;

        }
        /*
                public void ParseSupportedDatarefs()
                {
                    _dataRefs.Clear();
                    foreach (var dr in _connection.getDataRefDescriptions())
                    {
                        Type dataType;
                        switch (dr.DataType)
                        {
                            case "System.Boolean":
                                dataType = typeof(bool);
                                break;
                            case "System.Byte":
                                dataType = typeof(byte);
                                break;
                            case "System.SByte":
                                dataType = typeof(sbyte);
                                break;
                            case "System.Char":
                                dataType = typeof(char);
                                break;
                            case "System.Decimal":
                                dataType = typeof(decimal);
                                break;
                            case "System.Double":
                                dataType = typeof(double);
                                break;
                            case "System.Single":
                                dataType = typeof(float);
                                break;
                            case "System.Int32":
                                dataType = typeof(int);
                                break;
                            case "System.UInt32":
                                dataType = typeof(uint);
                                break;
                            case "System.IntPtr":
                                dataType = typeof(IntPtr);
                                break;
                            case "System.UIntPtr":
                                dataType = typeof(UIntPtr);
                                break;
                            case "System.Int64":
                                dataType = typeof(long);
                                break;
                            case "System.UInt64":
                                dataType = typeof(ulong);
                                break;
                            case "System.Int16":
                                dataType = typeof(short);
                                break;
                            case "System.UInt16":
                                dataType = typeof(ushort);
                                break;
                            case "System.String":
                                dataType = typeof(string);
                                break;
                        }

                        // Create ProsimInterface dataref class
                        lock (_dataRefs)
                        {
                            if(_dataRefs.ContainsKey(dr.Description))
                            {
                                continue;
                            }
                        }

                        var dataRef = new DataRef(dr.Name, 100, _connection, false);
                        dataRef.onDataChange += DataRef_onDataChange;

                        var item = new DataRefTableItem
                        {
                            DataRef = dataRef,
                            Description = dr.Description,
                            DataType = dr.DataType,
                            DataUnit = dr.DataUnit,
                            CanRead = dr.CanRead,
                            CanWrite = dr.CanWrite
                        };

                        lock (_dataRefs)
                        {
                            _dataRefs[dr.Name] = item;
                        }
                    }
                }

                public object ProsimGetVariable(string datarefName)
                {
                    if (!_dataRefs.ContainsKey(datarefName))
                    {
                        Logger.Log(LogLevel.Error, "ProsimInterface:GetValue", $"Dataref {datarefName} is not in Prosim database");
                    }

                    return _dataRefs[datarefName].Value;
                }

                public void ProsimSetVariable(string datarefName, object value)
                {
                    object _dataRefObj = GetDatarefObj(datarefName);
                    _dataRefObj.GetType().GetProperty("Value").SetValue(_dataRefObj, value);

                }

                public void SetValue(string datarefName, object value)
                {

                    if (!_dataRefs.ContainsKey(datarefName))
                    {
                        Logger.Log(LogLevel.Error, "ProsimInterface:SetValue", $"Dataref {datarefName} is not in Prosim database");
                    }

                    _dataRefs[datarefName].Value = value;
                }

                public object GetDatarefObj(string datarefName)
                {
                    if (!_dataRefs.ContainsKey(datarefName))
                    {
                        Logger.Log(LogLevel.Error, "ProsimInterface:GetDatarefObj", $"Dataref {datarefName} is not in Prosim database");
                    }

                    return _dataRefs[datarefName];
                }

                /// <summary>
                ///     Receive updates from DataRefs
                /// </summary>
                /// <param name="dataRef">The updated DataRef</param>
                private void DataRef_onDataChange(DataRef dataRef)
                {
                    if (IsDisposed)
                        return;

                    var name = dataRef.name;
                    DataRefTableItem item;

                    lock (_dataRefs)
                    {
                        // Check to make sure the DataRef is somewhere in the table
                        if (!_dataRefs.ContainsKey(name))
                            return;

                        // Get associated table item
                        item = _dataRefs[name];

                        if (checkBox1.Checked && item.Name.StartsWith("system.") && !item.Name.StartsWith("system.timers"))
                            MessageBox.Show($"Dataref [{name}] Value [{dataRef.value?.ToString()}]");
                    }

                    // Set the value of the table item to the new value
                    try

                    {
                        if (dataRef.value is Array)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (var arrayItem in (Array)dataRef.value)
                            {
                                if (sb.Length > 0)
                                    sb.Append(",");
                                sb.Append(arrayItem);
                            }
                            item.Value = sb.ToString();

                        }
                        else
                            item.Value = dataRef.value?.ToString();
                    }
                    catch (DataRefNotReady)
                    {
                    }

                    // Signal the DataRefTable to update the row, so the new value is displayed
                    try
                    {
                        BeginInvoke(new MethodInvoker(delegate
                        {
                            if (!IsDisposed)
                                DataRefTableItemBindingSource.ResetItem(item.Index);
                        }));
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }


                /// <summary>
                ///     When we connect to ProSim System, update the status label and start filling the table
                /// </summary>
                private void Connection_onConnect()
                {

                }

                /// <summary>
                ///     When we disconnect from ProSim System, update the status label
                /// </summary>
                private void Connection_onDisconnect()
                {

                }
        */
    }
}