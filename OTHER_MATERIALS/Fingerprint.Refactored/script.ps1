$containers = $(docker ps -a -f status=exited -q);
if ($containers){
    docker rm $containers;
}
echo 'y' | docker image prune -a
docker-compose -f "./fingerprint.yml" build
docker-compose -f "./fingerprint.yml" up
