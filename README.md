# BetterGraph by [svrooij](https://github.com/svrooij)

A pure C# PowerShell module to interact with the Microsoft Graph API.
Only supports PowerShell 7.4 and higher.

> [!NOTE]
> Creating a binary PowerShell module that supports both Windows PowerShell and PowerShell Core would not being able to use the latest c# features, which is why this module only supports PowerShell 7.4 and higher.
> It would be possible to create a binary module that supports both, but I don't care enough about legacy Windows PowerShell to do that.

## Installation

```powershell
Install-Module -Name Svrooij.BetterGraph -Repository PSGallery -Scope CurrentUser
```

## Usage

```powershell
Import-Module Svrooij.BetterGraph
# Connect to Microsoft Graph
Connect-BgGraph -Scopes "User.Read.All"

# Get first 10 users
Get-BgUser -Top 10

# Delete a user
Remove-BgUser -UserId "a1da9b7e-5dda-4a6e-ae11-1dd9fcf03614"
```

All commands in this module are prefixed with `Bg` to avoid conflicts with other modules. And all commands are cancelable py pressing `Ctrl+C` during execution.

### Paging

This module supports multiple ways of paging through results:

#### Automatic Paging

Wit automatic paging, it will asynchronously fetch all pages of results for you. And emit then when they are available.
If you pipe this result, you'll get the results user by user.

During the paging process, you can cancel the operation by pressing `Ctrl+C`.

```powershell
Get-BgUser -Top 10 -All -Select Id, DisplayName, UserPrincipalName | Format-Table DisplayName, UserPrincipalName
```

#### Manual Paging

With manual paging (not specifying `-All`), you can control the paging process yourself.
Each command that returns a collection of results and had another page available will return the results of the first page and set the `{NameOfCmd}NextLink` variable to the URL of the next page.

To get the next page, you can use the same command with the `-NextLink` parameter:

```powershell
# Get first page of users
Get-BgUser -Top 10 -Select Id, DisplayName, UserPrincipalName | Format-Table DisplayName, UserPrincipalName
while ($null -ne $GetBgUserNextLink -and "" -ne $GetBgUserNextLink) {
  # Get next page of users
  Get-BgUser -NextLink $GetBgUserNextLink | Format-Table DisplayName, UserPrincipalName
}
```

## Feedback

Since this is an experiment, I would love to hear your feedback on the module. Please start a discussion on the [GitHub repository](https://github.com/svrooij/BetterGraph-PowerShell/discussions).

## Developer notes

This project uses some of my other projects to smooth out making a modern PowerShell module:

- [Svrooij.PowerShell.DI](https://www.nuget.org/packages/SvRooij.PowerShell.DI#readme-body-tab) to generate the async plumbing, allow usage of `ILogger` and handles dependency injection.
- [Svrooij.PowerShell.Docs](https://www.nuget.org/packages/SvRooij.PowerShell.Docs#readme-body-tab) to generate the PowerShell documentation from the C# XML comments.
