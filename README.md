# Foto

## Prerequisites

- [dotnet 3.1.201 ](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [chocolatey](https://chocolatey.org/) OR [brew](https://brew.sh/)
- Helm 3 `choco install kubernetes-helm` OR `brew install helm` (only if you do k8s up and running)
- Skaffold `choco install skaffold` OR `brew install skaffold` (only if you do k8s up and running)
- Docker (with Kubernetes available, enable through docker preferences or settings)

## Up and Running

You can either do K8s OR docker compose, see the bottom of the readme for docker compose

```bash

  $ helm repo add stable https://kubernetes-charts.storage.googleapis.com/
  $ kubectl create ns ingress
  $ helm upgrade --atomic --install nginx-ingress-dev stable/nginx-ingress --namespace ingress --set controller.ingressClass=dev

  $ helm repo add bitnami https://charts.bitnami.com/bitnami
  $ kubectl create ns mongo
  $ kubectl create ns redis
  # the notes from the outputs hold the service names to connect to them
  $ helm install mongo-dev bitnami/mongodb --set usePassword=false --namespace mongo
  $ helm install redis-dev bitnami/redis --set usePassword=false  --namespace redis

  $ kubectl create ns foto
  # all of the above are one time only

  # this builds and deploys - run from root of repo
  $ skaffold run
  # this removes deployments (cleanup) - run from root of repo
  $ skaffold delete


  # if you want dev, realtime updates of containers etc
  # ctrl-c this process will cleanup!
  $ skaffold run

```

To view the UI, you should either be able to:

Goto: http://ui.foto.local

OR you'll have to update your hosts file

Elevated cmd / powershell
```
  $ notepad C:\Windows\System32\drivers\etc\hosts
```

> you really shouldn't have to on mac using chrome, it usally just works

Mac/linux
```
  $ sudo vim /etc/hosts
```

Add the line `127.0.0.1   ui.foto.local`

## Dotnet

### Configration

Override the configuration through environment variables

| Name | Value | Description |
|-|-|-|
| MongoHost | mongodb://mongo | Used to set the mongo database path |
| RedisHost | localhost | Used to set the redis cache path |

#### Setting through Powershell

```powershell
 > $env:MongoHost = 'whatever'
 > $env:RedisHost = 'whatever'
```

```bash
  $ export MongoHost=whatever
  $ export RedistHost=whatever
```

## React UI

See [documentation](./services/ui/app/README.md)

## Run using Docker

At root of repo

```bash
  # -d runs detached mode so it doesn't hog your console output or die when ctrl-c
  $ docker-compose up -d
  # add --build if you make any changes and want them to appear on the next up.

  # for cleanup
  $ docker-compose down 
```

Access the ui via `http://locahost:8080`

## How to test it is working

1. First load the UI up,  
2. Make sure the network tab is open
3. Drag a photo onto the Box, click Upload
4. Look in the network tab for `photo` request, click it and click the response tab, you should see a response like below:

```json
[
  {
    "id": "5ebc69794c9238000187c33b",
    "fileName": "DSC_0290.JPG",
    "gridFsId": "5ebc69794c9238000187c338",
    "contentType": "image/jpeg",
    "contentDisposition": "form-data; name=\"input0\"; filename=\"DSC_0290.JPG\""
  }
]
```

The `id` field means it has been saved into mongo! The `gridfsId` means the photo was saved into mongo gridFs