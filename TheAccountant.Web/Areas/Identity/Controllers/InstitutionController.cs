using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using TheAccountant.Models;
using TheAccountant.Models.ViewModels;

namespace TheAccountant.Areas.Identity.Controllers
{
    [Area("Identity")]
    [Route("Identity/[controller]/[action]")]
    [Authorize]
    public class InstitutionController : Controller
    {
        [HttpGet]
        public IActionResult AddPartial()
        {
            return PartialView("_AddInstitutionPartial");
        }

        [HttpPost]
        public IActionResult Add(InstitutionViewModel model)
        {
            if (!ModelState.IsValid)
                return PartialView("_AddInstitutionPartial", model);

            // TODO: Save to DB or link via API (e.g., Plaid integration)

            return Json(new { success = true });
        }

        [HttpGet]
        public IActionResult AuthorizeInstitution(string institution)
        {
            if (string.IsNullOrWhiteSpace(institution))
            {
                return BadRequest("Institution is required.");
            }

            // Map the institution to a partial view, model, or data if needed
            var viewName = institution.ToLower() switch
            {
                "chase" => "_AuthorizeChasePartial",
                "capitalone" => "_AuthorizeCapitalOnePartial",
                "bankofamerica" => "_AuthorizeBofAPartial",
                "iccu" => "_AuthorizeICCUPartial",
                _ => "_AuthorizeGenericPartial"
            };

            return PartialView(viewName, institution);
        }

        public IActionResult BeginAuthorization(string institution)
        {
            var redirectUri = Url.Action("AuthorizationCallback", "Institution", null, Request.Scheme);
            var state = Guid.NewGuid().ToString(); // Save state in session/db for CSRF protection

            // Normally this would come from a config or db
            var authUrl = institution.ToLower() switch
            {
                "capitalone" => $"https://api.capitalone.com/oauth/authorize?client_id=YOUR_CLIENT_ID&response_type=code&redirect_uri={Uri.EscapeDataString(redirectUri)}&state={state}&scope=accounts",
                _ => throw new Exception("Unknown institution")
            };

            return Redirect(authUrl);
        }

        public async Task<IActionResult> AuthorizationCallback(string code, string state)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Missing authorization code");

            // Exchange code for token
            var tokenResponse = await ExchangeCodeForTokenAsync(code);

            // Save token info to DB per user/session
            // var accessToken = tokenResponse.AccessToken;

            // Optionally: Fetch data now
            var accounts = await GetAccountsAsync(tokenResponse.AccessToken);

            return View("Accounts", accounts); // Or redirect
        }
        private async Task<TokenResponse> ExchangeCodeForTokenAsync(string code)
        {
            var client = new HttpClient();
            var response = await client.PostAsync("https://api.capitalone.com/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = "https://yourdomain.com/Institution/AuthorizationCallback",
                ["client_id"] = "YOUR_CLIENT_ID",
                ["client_secret"] = "YOUR_CLIENT_SECRET"
            }));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(json);
        }

        private async Task<List<Account>> GetAccountsAsync(string accessToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://api.capitalone.com/accounts/v1/me");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Account>>(json);
        }

        public async Task<TokenResponse> RefreshAccessTokenAsync(string refreshToken)
        {
            var client = new HttpClient();
            var response = await client.PostAsync("https://api.capitalone.com/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = "YOUR_CLIENT_ID",
                ["client_secret"] = "YOUR_CLIENT_SECRET"
            }));

            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(json);
        }

    }
}
