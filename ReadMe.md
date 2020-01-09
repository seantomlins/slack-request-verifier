# Usage

Add your Slack Signing Secret to your configuration

```json
{
  "SlackSigningSecret": "<YOUR_SECRET>"
}
```

Add an authorization policy, named "VerifySlack" in this example, and the SlackRequestHandler to the services
```c#
public void ConfigureServices( IServiceCollection services )
{
    services.AddMvc( );

    // ...

    services.AddAuthorization( x => 
        x.AddPolicy( "VerifyFromSlack", p => p.AddRequirements( new SlackVerifiedRequirement( ) ) )
    );

    services.AddSingleton<IAuthorizationHandler>( x =>
    {
        var slackSigningSecret = Configuration.GetValue<string>( "SlackSigningSecret" );
        return new SlackVerifiedHandler( slackSigningSecret );
    } );
}
```

Then add an Authorize attribute to your methods that you want to verify are from Slack, using the policy name
E.g. Slash command
```c#
[HttpPost]
[Authorize( Policy = "VerifyFromSlack" )]
public ActionResult Post( [FromForm] SlashCommand slashCommand ) 
{
    // ...
}
```