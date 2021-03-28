# ICAP Request Processing

ICAP Request Processing is a process to perform the Glasswall Rebuild functionality on the specified file. It also handles the sending of an outcome message to an RabbitMQ instance.

### Built With
- .NET Core
- Docker

### How it works
When it starts
- WHen it starts, it listens on the queue for a new file ready to get processed
- Once a message arrives to the queue, the .net processing module in called, file is processed and result is sent back  the queue
- Then program exits (In kubernetes environment it should be recreated by the controller)

## Configuration
- This pod don't mount any persistent volume


### Docker build
- To build the docker image
```
git clone https://github.com/k8-proxy/go-k8s-srv1.git
cd k8-proxy/go-k8s-srv1
docker build -t <docker_image_name> .
```

- To run the container
First make sure that you have rabbitmq and minio running, then run the command bellow 

```
docker run -e ADAPTATION_REQUEST_QUEUE_HOSTNAME='<rabbit-host>' \ 
-e ADAPTATION_REQUEST_QUEUE_PORT='<rabbit-port>' \
-e MESSAGE_BROKER_USER='<rabbit-user>' \
-e MESSAGE_BROKER_PASSWORD='<rabbit-password>' \
-e MINIO_ENDPOINT='<minio-endpoint>' \ 
-e MINIO_ACCESS_KEY='<minio-access>' \ 
-e MINIO_SECRET_KEY='<minio-secret>' \ 
-e MINIO_SOURCE_BUCKET='<bucket-to-upload-file>' \ 
--name <docker_container_name> <docker_image_name>
```

# Testing steps

- Run the container as mentionned above

- Publish data reference to rabbitMq on queue name : adaptation-request-queue with the following data(table) :
* file-id : An ID for the file
* source-file-location : The full path to the file
* rebuilt-file-location : A full path representing the location where the rebuilt file will go to


- Check your container logs to see the processing

```
docker logs <container name>
```

# Rebuild flow to implement

![new-rebuild-flow-v2](https://github.com/k8-proxy/go-k8s-infra/raw/main/diagram/go-k8s-infra.png)