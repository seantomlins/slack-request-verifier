using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

public sealed class SlackRequestHandler : AuthorizationHandler<SlackVerifiedRequirement>
{
	private readonly SlackRequestVerifier _slackRequestVerifier;

	public SlackVerifiedHandler( string slackSigningSecret, int maxAgeSeconds = 300 )
	{
		_slackRequestVerifier = new SlackRequestVerifier( slackSigningSecret, maxAgeSeconds );
	}

	protected override Task HandleRequirementAsync( AuthorizationHandlerContext context, SlackVerifiedRequirement requirement )
	{
		if ( context.Resource is AuthorizationFilterContext mvcContext )
		{
			if ( _slackRequestVerifier.IsFromSlack( mvcContext.HttpContext.Request ) )
			{
				context.Succeed( requirement );
			}
		}

		return Task.CompletedTask;
	}

	private sealed class SlackRequestVerifier
	{
		private const string XSlackRequestTimestamp = "X-Slack-Request-Timestamp";
		private const string XSlackSignature = "X-Slack-Signature";

		private readonly string _signingSecret;
		private readonly int _maxAgeSeconds;

		public SlackRequestVerifier( string slackSigningSecret, int maxAgeSeconds )
		{
			_signingSecret = slackSigningSecret;
			_maxAgeSeconds = maxAgeSeconds;
		}

		public bool IsFromSlack( HttpRequest request )
		{
			var slackTimestamp = request.Headers[ XSlackRequestTimestamp ].ToString( );
			var slackSignature = request.Headers[ XSlackSignature ].ToString( );

			if ( string.IsNullOrEmpty( slackTimestamp )
					|| string.IsNullOrEmpty( slackSignature )
					|| DateTimeOffset.Now.ToUnixTimeSeconds( ) - long.Parse( slackTimestamp ) > _maxAgeSeconds ) // It could be a replay attack, so let's ignore it.
			{
				return false;
			}

			var formFields = request.Form.Select( x => $"{x.Key}={System.Net.WebUtility.UrlEncode( x.Value )}" );

			var requestBody = string.Join( '&', formFields );

			var baseString = string.Join( ':', "v0", slackTimestamp, requestBody );

			var mySignature = "v0=" + GetHash( _signingSecret, baseString );

			return string.Compare( slackSignature, mySignature, StringComparison.InvariantCulture ) == 0;
		}

		private static string GetHash( string key, string text )
		{
			using ( var hash = new HMACSHA256( Encoding.UTF8.GetBytes( key ) ) )
			{
				var hashBytes = hash.ComputeHash( Encoding.UTF8.GetBytes( text ) );
				return BitConverter.ToString( hashBytes ).Replace( "-", "" ).ToLower( );
			}
		}
	}
}

public sealed class SlackVerifiedRequirement : IAuthorizationRequirement { }