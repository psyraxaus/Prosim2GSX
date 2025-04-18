using ProSimSDK;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Prosim2GSX.Models;

namespace Prosim2GSX
{
    public class ProsimInterface
    {
        protected ServiceModel Model;
        protected ProSimConnect Connection;

        public ProsimInterface(ServiceModel model, ProSimConnect _connection)
        {
            Model = model;
            Connection = _connection;
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

        public dynamic GetProsimVariable(string _dataRef)
        {
            Logger.Log(LogLevel.Debug, "ProsimInterface:GetProsimVariable", $"Attempting to get dataref: {_dataRef}");
            try
            {
                return Connection.ReadDataRef(_dataRef);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:ReadDataRef", $"There was an error reading {_dataRef} - exception {ex.ToString()}");
                return null;
            }
        }

        public void SetProsimVariable(string _dataRef, object value)
        {
            DataRef dataRef = new DataRef(_dataRef, 100, Connection);
            Logger.Log(LogLevel.Debug, "ProsimInterface:SetProsimVariable", $"Attempting to set dataref: {_dataRef}");
            try
            {
                dataRef.value = value;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:SetProsimSetVariable", $"There was an error setting {_dataRef} value {value} - exception {ex.ToString()}");
            }
        }

        /// <summary>
        /// Get the Prosim backend URL
        /// </summary>
        /// <returns>The backend URL</returns>
        public string GetBackendUrl()
        {
            // Return the hardcoded URL for Prosim's backend
            return "http://127.0.0.1:5000/efb";
        }

        /// <summary>
        /// Make a POST request to the Prosim backend
        /// </summary>
        /// <param name="url">The URL to post to</param>
        /// <param name="jsonContent">The JSON content to post</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> PostAsync(string url, string jsonContent)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    
                    StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    
                    Logger.Log(LogLevel.Debug, "ProsimInterface:PostAsync", $"Posting to {url}");
                    HttpResponseMessage response = await client.PostAsync(url, content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(LogLevel.Debug, "ProsimInterface:PostAsync", $"POST to {url} successful");
                        return true;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error, "ProsimInterface:PostAsync", 
                            $"POST to {url} failed with status code {response.StatusCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:PostAsync", $"Error posting to {url}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Make a DELETE request to the Prosim backend
        /// </summary>
        /// <param name="url">The URL to delete from</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteAsync(string url)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    
                    Logger.Log(LogLevel.Debug, "ProsimInterface:DeleteAsync", $"Deleting from {url}");
                    HttpResponseMessage response = await client.DeleteAsync(url);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Logger.Log(LogLevel.Debug, "ProsimInterface:DeleteAsync", $"DELETE to {url} successful");
                        return true;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error, "ProsimInterface:DeleteAsync", 
                            $"DELETE to {url} failed with status code {response.StatusCode}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, "ProsimInterface:DeleteAsync", $"Error deleting from {url}: {ex.Message}");
                return false;
            }
        }
    }
}
