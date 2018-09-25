# Bamboo plug

The Bamboo plug provides an interface to perform actions in a remote Bamboo
server for the Plastic SCM DevOps system.

This is the source code used by the actual built-in Bamboo plug. Use it as a reference
to build your own CI plug!

# Build
The executable is built from .NET Framework code using the provided `src/bambooplug.sln`
solution file. You can use Visual Studio or MSBuild to compile it.

**Note:** We'll use `${DEVOPS_DIR}` as alias for `%PROGRAMFILES%\PlasticSCM5\server\devops`
in *Windows* or `/var/lib/plasticscm/devops` in *macOS* or *Linux*.

# Setup
If you just want to use the built-in Bamboo plug you don't need to do any of this.
The Bamboo plug is available as a built-in plug in the DevOps section of the WebAdmin.
Open it up and configure your own!

## Configuration files
You'll notice some configuration files under `/src/configuration`. Here's what they do:
* `bambooplug.log.conf`: log4net configuration. The output log file is specified here. This file should be in the binaries output directory.
* `ci-bambooplug.definition.conf`: plug definition file. You'll need to place this file in the Plastic SCM DevOps directory to allow the system to discover your Bamboo plug.
* `bambooplug.config.template`: mergebot configuration template. It describes the expected format of the Bamboo plug configuration. We recommend to keep it in the binaries output directory
* `bambooplug.conf`: an example of a valid Bamboo plug configuration. It's built according to the `bambooplug.config.template` specification.

## Add to Plastic SCM Server DevOps
To allow Plastic SCM Server DevOps to discover your custom Bamboo plug, just drop 
the `ci-bambooplug.definition.conf` file in `${DEVOPS_DIR}/config/plugs/available$`.
Make sure the `command` and `template` keys contain the appropriate values for
your deployment!

# Behavior
The **Bamboo plug** provides an API for **mergebots** to connect to Bamboo.
They use the plug to launch builds in a Bamboo server and retrieve the build status.

## What the configuration looks like
When a mergebot requires a CI plug to work, you can select a Bamboo Plug Configuration.

<p align="center">
  <img alt="CI plug select" src="https://raw.githubusercontent.com/mig42/bambooplug/master/doc/img/ci-plug-select.png" />
</p>

You can either select an existing configuration or create a new one.

When you create a new Bamboo Plug Configuration, you have to fill in the following values:

<p align="center">
  <img alt="Bambooplug configuration example"
       src="https://raw.githubusercontent.com/mig42/bambooplug/master/doc/img/configuration-example.png" />
</p>

## Installation requirements - The Bamboo Lightweight Plugin
**⚠️ Important! ⚠️**

Please make sure that you've installed our lightweight Bamboo plugin before you create
a new configuration for a server. You can find it in the **client** install
directory (`%PROGRAMFILES%\PlasticSCM5\client` in Windows, `/opt/plasticscm5/client`
in Linux or `/Applications/PlasticSCM.app/Contents/MonoBundle` in macOS),
inside the `mergebot-zeroconf-plugins` directory.

You'll also need to install a Plastic SCM CLI Client (version **7.0.16.2200** or higher)
in the Bamboo machine. It's required to perform all SCM operations against the server
(e.g. update the Bamboo Plastic SCM workspace). The user account running Bamboo will need
a valid Plastic SCM Client configuration to contact the target Plastic SCM Server.

## Bamboo Configuration
The lightweight Bamboo plugin makes it unnecessary to specify repositories in Bamboo. Instead,
add a `Mergebot Plastic SCM` step as the first one in your build configuration.
The `mergebot` will take care of the rest!

When you create a new Bamboo plan, leave the repository set as *None*.

<p align="center">
  <img alt="Plan repository"
       src="https://raw.githubusercontent.com/mig42/bambooplug/master/doc/img/plan-repository.png" />
</p>

Then, add a **Mergebot Plastic SCM Checkout** step as the first one

<p align="center">
  <img alt="Create the Mergebot Plastic SCM Checkout step"
       src="https://raw.githubusercontent.com/mig42/bambooplug/master/doc/img/build-step-create.png" />
</p>

Finally, configure the build step if you need some fine tuning.

<p align="center">
  <img alt="Configure the build step"
       src="https://raw.githubusercontent.com/mig42/bambooplug/master/doc/img/build-step-configure.png" />
</p>

When the **mergebot** requests a new build run or an existing build status
from the **Bamboo plug**, it calls the remote Bamboo API using the URL and
credentials in the plug configuration.

## How it works

When a user creates a new **Bamboo plug** configuration, by default it executes
the built-in plug binaries using the values of the configuration. Then, it automatically
connects to the Plastic SCM server through a *websocket* and stands by for requests.
You can also choose not to automatically run that particular configuration if you don't want to.

# Support
If you have any questions about this plug don't hesitate to contact us by
[email](support@codicesoftware.com) or in our [forum](http://www.plasticscm.net)!
