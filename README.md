# CS370-SoftwareEngineering Project

> Design document by T.VanSlyke
>
> Initial draft: 9/15/2021 - Last update: 9/15/2021
> 
> Reviewers: N/A
>
> [Github link](https://github.com/trentv4/CS370-SoftwareEngineering)

# Overview

[Design document](https://docs.google.com/document/d/1gYEqKsX7-bFS0qfd3JbI5buF9eBmvD9uDNgfBKHmUww/edit?usp=sharing)

[Project requirements image](https://media.discordapp.net/attachments/881975639568171081/882318797158113320/Capture.PNG)

# How to install and build

If you are looking to install the game, you can download it at [the downloads page](). Unzip the file with your preferred tool and then run `FaceTheFuture.exe`

For developers: 

* Clone the repository
* The repository is already configured for a Visual Studio Code workflow with appropriate gitignores. If you want to use another editor, you will have to configure that yourself. 
* Build with `dotnet build` 
* Run with `dotnet run`

When you publish, calling `dotnet publish` will create a clean folder in the `build/` directory that can be zipped and send to users.