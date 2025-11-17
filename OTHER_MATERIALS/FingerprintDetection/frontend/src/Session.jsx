import {useEffect, useState} from "react";
import {useParams} from "react-router-dom";
import {Chip, ListItem, Paper} from "@mui/material";

const Session = () => {
    const {session} = useParams();
    const [clusters, setClusters] = useState([]);
    const [pairs, setPairs] = useState([]);

    useEffect(() => {
        const fetchClusters = async () => {
            const response = await fetch(`http://localhost:5098/sessions/${session}`);
            const clusterPairs = await response.json();
            setPairs(clusterPairs);
            const uniqueClusters = [...clusterPairs.map(x => x.firstClusterId), ...clusterPairs.map(x => x.secondClusterId)];
            setClusters([...new Set(uniqueClusters)]);
        };
        fetchClusters().then(() => console.log("Done"));
    }, []);

    return (
        <>
            <h1 style={{
                marginTop: 0
            }}>Session id {session}</h1>
            <h1>Clusters</h1>
            <Paper
                sx={{
                    display: 'flex',
                    flexWrap: "wrap",
                    background: 'rgba(255, 255, 255, 0)',
                    justifyContent: 'center',
                    margin: 0
                }}
                component="ul"
            >
                {clusters.map((data) => {
                    return (
                        <ListItem key={data}>
                            <Chip
                                label={data}
                                component="a"
                                href={`/cluster/${session}/${data}`}
                                variant="outlined"
                                clickable
                            />
                        </ListItem>
                    );
                })}
            </Paper>
            <h1>Comparisons</h1>
            <Paper
                sx={{
                    display: 'flex',
                    flexWrap: "wrap",
                    background: 'rgba(255, 255, 255, 0)',
                    justifyContent: 'center',
                    margin: 0
                }}
                component="ul"
            >
                {pairs.map((data) => {
                    return (
                        <ListItem key={`${data.firstClusterId}${data.secondClusterId}`}>
                            <Chip
                                label={`${data.firstClusterId}-${data.secondClusterId}`}
                                component="a"
                                href={`/comparison/${session}/${data.firstClusterId}/${data.secondClusterId}`}
                                variant="outlined"
                                clickable
                            />
                        </ListItem>
                    );
                })}
            </Paper>
        </>
    )
};

export default Session;