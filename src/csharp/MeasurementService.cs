using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json.Linq;

public class MeasurementService
{
    private readonly HttpClient _httpClient;

    public MeasurementService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> IssueMeasurementCommandWithParamsAsync(string id, string command, JObject parameters)
    {
        // Validate boolean parameters
        var booleanParams = new[] { "include cal", "append lf tail", "append hf tail", "frequency warping", "replicate data" };
        foreach (var param in booleanParams)
        {
            if (parameters.ContainsKey(param) && !parameters[param].Type.Equals(JTokenType.Boolean))
            {
                return $"Error: Invalid value for parameter '{param}'. Expected a boolean value.";
            }
        }

        // Validate numeric parameters
        if (parameters.ContainsKey("lf tail slope") && parameters["lf tail slope"].Value<double>() < 0)
        {
            return "Error: Invalid value for parameter 'lf tail slope'. Must be ≥ 0.";
        }
        if (parameters.ContainsKey("hf tail slope") && parameters["hf tail slope"].Value<double>() > 0)
        {
            return "Error: Invalid value for parameter 'hf tail slope'. Must be ≤ 0.";
        }

        // Validate required parameters
        if (parameters["append lf tail"].Value<bool>())
        {
            if (!parameters.ContainsKey("lf tail start") || !parameters.ContainsKey("lf tail slope"))
            {
                return "Error: Missing required parameters 'lf tail start' and/or 'lf tail slope'.";
            }
        }
        if (parameters["append hf tail"].Value<bool>())
        {
            if (!parameters.ContainsKey("hf tail start") || !parameters.ContainsKey("hf tail slope") || !parameters.ContainsKey("frequency warping"))
            {
                return "Error: Missing required parameters 'hf tail start', 'hf tail slope', and/or 'frequency warping'.";
            }
        }
        if (!parameters["append lf tail"].Value<bool>() || !parameters["append hf tail"].Value<bool>())
        {
            if (!parameters.ContainsKey("replicate data"))
            {
                return "Error: Missing required parameter 'replicate data'.";
            }
        }

        if (command == "Smooth")
        {
            if (!parameters.ContainsKey("smoothing") || !parameters["smoothing"].Type.Equals(JTokenType.String))
            {
                return "Error: Missing or invalid 'smoothing' parameter for 'Smooth' command. Expected a string.";
            }
        }

        if (command == "Offset t=0")
        {
            if (!parameters.ContainsKey("offset") || !parameters["offset"].Type.Equals(JTokenType.String))
            {
                return "Error: Missing or invalid 'offset' parameter for 'Offset t=0' command. Expected a string.";
            }
            if (!parameters.ContainsKey("unit") || !parameters["unit"].Type.Equals(JTokenType.String))
            {
                return "Error: Missing or invalid 'unit' parameter for 'Offset t=0' command. Expected a string.";
            }
        }

        if (command == "Generate RT60")
        {
            if (!parameters.ContainsKey("octaveFrac") || !parameters["octaveFrac"].Type.Equals(JTokenType.Integer))
            {
                return "Error: Missing or invalid 'octaveFrac' parameter for 'Generate RT60' command. Expected a number.";
            }
            if (!parameters.ContainsKey("filterOrder") || !parameters["filterOrder"].Type.Equals(JTokenType.Integer))
            {
                return "Error: Missing or invalid 'filterOrder' parameter for 'Generate RT60' command. Expected a number.";
            }
            if (!parameters.ContainsKey("zeroPhaseFiltered") || !parameters["zeroPhaseFiltered"].Type.Equals(JTokenType.Boolean))
            {
                return "Error: Missing or invalid 'zeroPhaseFiltered' parameter for 'Generate RT60' command. Expected a boolean.";
            }
            if (!parameters.ContainsKey("reverseFiltered") || !parameters["reverseFiltered"].Type.Equals(JTokenType.Boolean))
            {
                return "Error: Missing or invalid 'reverseFiltered' parameter for 'Generate RT60' command. Expected a boolean.";
            }
        }

        var content = new StringContent("{\"command\":\"" + command + "\",\"parameters\":" + parameters.ToString() + "}", Encoding.UTF8, "application/json");
        string endpoint = $"/measurements/{id}/command";
        return await PostAsync(endpoint, content);
    }

    public async Task<string> GenerateWaterfallAsync(string mode, int slices, JObject parameters)
    {
        // Validate required parameters for "Fourier" mode
        if (mode == "Fourier")
        {
            var requiredParams = new[] { "left window type", "right window type", "window width ms", "time range ms", "rise time ms", "use csd mode" };
            foreach (var param in requiredParams)
            {
                if (!parameters.ContainsKey(param))
                {
                    return $"Error: Missing required parameter '{param}' for 'Fourier' mode.";
                }
            }
        }
        // Validate required parameters for "Burst decay" mode
        else if (mode == "Burst decay")
        {
            var requiredParams = new[] { "bandwidth", "periods" };
            foreach (var param in requiredParams)
            {
                if (!parameters.ContainsKey(param))
                {
                    return $"Error: Missing required parameter '{param}' for 'Burst decay' mode.";
                }
            }
        }
        else
        {
            return "Error: Invalid mode. Expected 'Fourier' or 'Burst decay'.";
        }

        var content = new StringContent("{\"command\":\"Generate waterfall\",\"parameters\":{\"mode\":\"" + mode + "\",\"slices\":" + slices + "," + parameters.ToString() + "}}", Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/waterfalls", content);
    }

    public async Task<string> GenerateSpectrogramAsync(string mode, int slices, JObject parameters)
    {
        // Validate the "mode" parameter
        var validModes = new[] { "Fourier", "Wavelet", "Airy CWT", "Morlet CWT", "Burst decay" };
        if (Array.IndexOf(validModes, mode) == -1)
        {
            return "Error: Invalid value for parameter 'mode'.";
        }

        // Validate the "slices" parameter
        if (slices <= 0)
        {
            return "Error: Invalid value for parameter 'slices'. Must be > 0.";
        }

        // Validate the "ppo" parameter
        if (!parameters.ContainsKey("ppo") || !parameters["ppo"].Type.Equals(JTokenType.Integer))
        {
            return "Error: Missing or invalid 'ppo' parameter. Expected a number.";
        }

        // Validate the "amplitude" parameter
        if (!parameters.ContainsKey("amplitude") || !parameters["amplitude"].Type.Equals(JTokenType.String))
        {
            return "Error: Missing or invalid 'amplitude' parameter. Expected a string.";
        }

        // Validate additional parameters required for different modes
        if (mode == "Fourier")
        {
            var requiredParams = new[] { "window type", "window width ms", "before ms", "after ms" };
            foreach (var param in requiredParams)
            {
                if (!parameters.ContainsKey(param))
                {
                    return $"Error: Missing required parameter '{param}' for 'Fourier' mode.";
                }
            }
        }
        else if (mode == "Burst decay")
        {
            var requiredParams = new[] { "bandwidth", "periods" };
            foreach (var param in requiredParams)
            {
                if (!parameters.ContainsKey(param))
                {
                    return $"Error: Missing required parameter '{param}' for 'Burst decay' mode.";
                }
            }
        }
        else if (mode == "Wavelet" || mode == "Airy CWT" || mode == "Morlet CWT")
        {
            var requiredParams = new[] { "bandwidth", "before ms", "after ms" };
            foreach (var param in requiredParams)
            {
                if (!parameters.ContainsKey(param))
                {
                    return $"Error: Missing required parameter '{param}' for '{mode}' mode.";
                }
            }
        }

        var content = new StringContent("{\"command\":\"Generate spectrogram\",\"parameters\":{\"mode\":\"" + mode + "\",\"slices\":" + slices + "," + parameters.ToString() + "}}", Encoding.UTF8, "application/json");
        return await PostAsync("http://localhost:4735/spectrograms", content);
    }

    private async Task<string> PostAsync(string endpoint, HttpContent content)
    {
        HttpResponseMessage response = await _httpClient.PostAsync(endpoint, content);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}
