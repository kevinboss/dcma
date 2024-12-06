<p align="center">
  <img src="https://github.com/kevinboss/port/blob/master/logo_1.png" align="left" style="margin-right: 10px;" />
</p>

[![CI](https://github.com/kevinboss/port/actions/workflows/ci.yaml/badge.svg?event=push)](https://github.com/kevinboss/port/actions/workflows/ci.yaml)
[![CI](https://raw.githubusercontent.com/kevinboss/heartbeat/main/badges/kevinboss_port.svg)](https://github.com/kevinboss/heartbeat)

run and manage docker images with ease. Create snapshots from running containers, reset container to their inital image and save the state of running containers without the need to remember docker cli commands, even when using a remote docker engine.

<br/><br/><br/><br/><br/><br/><br/><br/><br/><br/>

![Example](https://github.com/kevinboss/port/raw/master/example-2.gif)

## How to get it

### Install using [scoop](https://scoop.sh)

`scoop bucket add maple 'https://github.com/kevinboss/maple.git'`

`scoop install port`

### Install using [winget](https://learn.microsoft.com/en-us/windows/package-manager/winget/) 

`winget install kevinboss.port`

### Install manually

[Latest release 💾](https://github.com/kevinboss/port/releases/latest)

Then add folder to path `$Env:PATH += ";C:\Path\To\Folder"`

## How to configure it

```yaml
version: 1.1
dockerEndpoint: unix:///var/run/docker.sock
imageConfigs:
  - identifier: Getting.Started
    imageName: docker/getting-started
    imageTags:
      - latest
      - vscode
    ports:
      - 80:80
    environment:
      - DEBUG=1
```

A default .port file will be created in your user profile if you don't manually create one

## Powershell

To get Unicode support in Powershell, add 

`[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()`
 
to your $profile.

## Commands Overview

### Run an Image
- **Syntax**: `run [identifier] -r`
- **Description**: Executes a specified tag (base or snapshot) of an image.
- **Parameters**:
  - `identifier` (optional): If omitted, a prompt will request image selection.
  - `-r` (reset) (optional): Resets the existing container for the specified image, if applicable.

### List Images
- **Syntax**: `list [identifier]`
- **Description**: Displays all images and their tags.
- **Parameters**:
  - `identifier` (optional): Limits the listing to images under the given identifier. Without it, all images are listed.

### Commit a Container
- **Syntax**: `commit -t [identifier]`
- **Description**: Generates an image from the currently active container.
- **Parameters**:
  - `identifier` (optional): If omitted, a prompt will request container selection.
  - `-t` (tag) (optional): Specifies the tag name. Defaults to the current date-time if not provided.

### Reset a Container
- **Syntax**: `reset [identifier]`
- **Description**: Stops, removes, and recreates the container using its original image.
- **Parameters**:
  - `identifier` (optional): If omitted, a prompt will request container selection.

### Remove an Image
- **Syntax**: `remove -r [identifier]`
- **Description**: Deletes a specified image tag (base, snapshot, or untagged).
- **Parameters**:
  - `identifier` (optional): If omitted, a prompt will request image selection.
  - `-r` (recursive) (optional): Automatically deletes child images. Without this, an error is raised if the image has dependents.

### Pull an Image
- **Syntax**: `pull [identifier]`
- **Description**: Downloads a specified tag (base or snapshot) of an image.
- **Parameters**:
  - `identifier` (optional): If omitted, a prompt will request image selection.

### Prune Images
- **Syntax**: `prune [identifier]`
- **Description**: Removes untagged versions of an image.
- **Parameters**:
  - `identifier` (optional): If omitted, a prompt will request image selection.

### Stop a Container
- **Syntax**: `stop [identifier]`
- **Description**: Halts the operation of the currently active container.
- **Parameters**:
  - `identifier` (optional): Specifies the container to stop. If omitted, operates on the current container.
