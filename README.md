# Foto

## Prerequisites

- [dotnet 3.1.201 ](https://dotnet.microsoft.com/download/dotnet-core/3.1)
- [chocolatey](https://chocolatey.org/) OR [brew](https://brew.sh/)
- Helm 3 `choco install kubernetes-helm` OR `brew install helm`
- Skaffold `choco install skaffold` OR `brew install skaffold`
- Docker (with Kubernetes available, enable through docker preferences or settings)

## Up and Running

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

See (documentation)[./services/ui/app]