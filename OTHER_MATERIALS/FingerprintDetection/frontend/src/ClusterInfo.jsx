import {useParams} from "react-router-dom";
import Cluster from "./Cluster.jsx";
import Tables from "./Tables.jsx";

const ClusterInfo = (props) => {
    const {session, cluster} = useParams();

    return (<div style={{
        display: "flex",
        flexDirection: "column",
        justifyContent: "center"
    }}>
        <Cluster session={session} cluster={cluster} />
        <Tables session={session} cluster={cluster} />
    </div>);
}

export default ClusterInfo;