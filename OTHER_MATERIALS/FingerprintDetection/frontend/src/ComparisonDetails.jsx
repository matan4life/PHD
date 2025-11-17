import {Component} from "react";
import {Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow} from "@mui/material";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {useParams} from "react-router-dom";
import {faSquareCheck, faSquareXmark} from "@fortawesome/free-solid-svg-icons";
import * as d3 from "d3";

const Wrapper = () => {
    const {session, firstCluster, secondCluster, firstIndex, secondIndex} = useParams();
    return (<ComparisonDetails
        session={session}
        firstCluster={firstCluster}
        secondCluster={secondCluster}
        firstPosition={firstIndex}
        secondPosition={secondIndex}
    />);
}

class ComparisonDetails extends Component {
    constructor(props) {
        super(props);
        this.state = {
            details: []
        };
    }

    async componentDidMount() {
        await this.drawDetails();
    }


    async drawDetails() {
        const {session, firstCluster, secondCluster, firstPosition, secondPosition} = this.props;
        const response = await fetch(`http://localhost:5098/sessions/${session}/${firstCluster}/${secondCluster}/${firstPosition}/${secondPosition}`);
        const details = await response.json();
        this.setState({
            details: details.telemetry
        });
        const margin = {top: 30, right: 30, bottom: 30, left: 40};
        const viewBox = {x: 0, y: 0, w: 500, h: 500};
        const width = viewBox.w - margin.left - margin.right;
        const height = viewBox.h - margin.top - margin.bottom;
        const xRange = [0, 300];
        const xAxis = d3.scaleLinear()
            .domain(xRange)         // values from our domain (0 to 10)
            .range([0, width]);     // will be assigned a valid x coordinate
        const yRange = [0, 300];
        const yAxis = d3.scaleLinear()
            .domain(yRange)         // remember that in SVG the y axis points downwards
            .range([height, 0]);    // but we want our axis pointing upwards, like a normal damn axis
        const color = d3.scaleOrdinal()
            .domain([...Array(20).keys()])
            .range(["#1f77b4", "#60a6d6", "#ff7f0e", "#ffbb78", "#2ca02c", "#98df8a", "#d62728", "#ff9896", "#9467bd", "#c5b0d5", "#8c564b", "#c49c94", "#e377c2", "#f7b6d2", "#7f7f7f", "#c7c7c7", "#bcbd22", "#dbdb8d", "#17becf", "#9edae5"]);
        const line = d3.line((d) => xAxis(d.x), d => yAxis(d.y));
        const firstClusterResponse = await fetch(`http://localhost:5098/descriptors/${session}/${firstCluster}`);
        const firstClusterDetails = await firstClusterResponse.json();
        const secondClusterResponse = await fetch(`http://localhost:5098/descriptors/${session}/${secondCluster}`);
        const secondClusterDetails = await secondClusterResponse.json();
        console.log(details.firstIndices);
        console.log(details.secondIndices);
        const firstCentroid = {
            x: firstClusterDetails.Center.X,
            y: xRange[1] - firstClusterDetails.Center.Y,
            cluster: 0
        };
        let lastUsedFirstCluster = 2;
        const firstPoints = firstClusterDetails.Distances.map((point, index) => ({
            x: point[0],
            y: xRange[1] - point[1],
            cluster: (details.firstIndices?.findIndex(i => index === i) ?? -1) + 2
        }));
        const secondCentroid = {
            x: secondClusterDetails.Center.X,
            y: xRange[1] - secondClusterDetails.Center.Y,
            cluster: 0
        };
        let lastUsedSecondCluster = 2;
        const secondPoints = secondClusterDetails.Distances.map((point, index) => ({
            x: point[0],
            y: xRange[1] - point[1],
            cluster: (details.secondIndices?.findIndex(i => index === i) ?? -1) + 2
        }));
        const equals = (point, d) => {
            return point[0] === d.x && d.y === xRange[1] - point[1];
        }
        d3.select("#firstCluster svg").remove();
        d3.select("#secondCluster svg").remove();
        const firstSvg = d3.select('#firstCluster')
            .append('svg')
            .attr('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`)
            .attr('width', window.innerWidth / 2 - margin.left - margin.right)
            .attr('height', window.innerHeight - margin.top - margin.bottom)
            .append('g')
            .attr('transform', `translate(${margin.left}, ${margin.top})`)     // mind the margins
            .attr('color', '#000000')
            .attr('font-weight', 'bold')                                       // we are bold enough to do this
            .attr('stroke-width', 2);
        firstSvg.append('g')
            .attr("class", "x axis-grid")
            .attr('transform', 'translate(0,' + height + ')')
            .call(d3.axisBottom(xAxis).tickSize(-height).tickFormat('').ticks(10));
        firstSvg.append('g')
            .attr("class", "y axis-grid")
            .call(d3.axisLeft(yAxis).tickSize(-width).tickFormat('').ticks(10));
        const xAxisValueFirst = firstSvg.append('g')
            .attr('transform', `translate(0, ${height})`)
            .call(d3.axisBottom(xAxis));
        xAxisValueFirst.selectAll(".tick text")
            .attr("rotate", "15")
            .attr("font-family", "cursive")
            .attr("font-size", "14");
        const yAxisValueFirst = firstSvg.append('g')
            .call(d3.axisLeft(yAxis));
        yAxisValueFirst.selectAll(".tick text")
            .attr("rotate", "15")
            .attr("font-family", "cursive")
            .attr("font-size", "14");
        for (const point of firstPoints.filter(point => point.cluster > 1 || firstClusterDetails.Distances.findIndex(clusterPoint => equals(clusterPoint, point)) === details.telemetry[0].FirstIndex)) {
            const index = firstClusterDetails.Distances.findIndex(clusterPoint => equals(clusterPoint, point));
            let color = "#FFFFFF";
            if (index === details.telemetry[0].FirstIndex){
                color = "#000000";
            }
            firstSvg.append("path")
                .attr("d", line([firstCentroid, point]))
                .attr("stroke", color);
        }
        const pointsSvgFirst = firstSvg.append('g')          // place them in a group, so they don't run away
            .attr('id', 'points-svg')              // assign them an id, taking away their individuality
            .selectAll('dot')
            .data(firstPoints)                          // loop over our data
            .join('circle')                        // add a circle
            .attr('cx', d => xAxis(d.x))               // position
            .attr('cy', d => yAxis(d.y))
            .attr('r', 3)                          // radius
            .style('fill', d => color(d.cluster));
        firstSvg.select("#points-svg")
            .selectAll('dot')
            .data(firstPoints)
            .join('text')
            .attr('dx', d => xAxis(d.x) - 2)
            .attr('dy', d => yAxis(d.y) - 5)
            .attr('font-size', '8px')
            .attr('font-family', 'cursive')
            .text(d => firstClusterDetails.Distances.findIndex(point => equals(point, d)));
        const centroidsSvgFirst = firstSvg.append('g')
            .attr('id', 'centroids-svg')
            .selectAll('dot')
            .data([firstCentroid])
            .join('circle')
            .attr('cx', d => xAxis(d.x))
            .attr('cy', d => yAxis(d.y))
            .attr('r', 5)                       // a bit bigger than data points
            .style('fill', '#e6e8ea')           // greyish fill
            .attr('stroke', (d, i) => color(i)) // and a thick colorful outline
            .attr('stroke-width', 3);
        const secondSvg = d3.select('#secondCluster')
            .append('svg')
            .attr('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`)
            .attr('width', window.innerWidth / 2 - margin.left - margin.right)
            .attr('height', window.innerHeight - margin.top - margin.bottom)
            .append('g')
            .attr('transform', `translate(${margin.left}, ${margin.top})`)     // mind the margins
            .attr('color', '#000000')
            .attr('font-weight', 'bold')                                       // we are bold enough to do this
            .attr('stroke-width', 2);

        secondSvg.append('g')
            .attr("class", "x axis-grid")
            .attr('transform', 'translate(0,' + height + ')')
            .call(d3.axisBottom(xAxis).tickSize(-height).tickFormat('').ticks(10));
        secondSvg.append('g')
            .attr("class", "y axis-grid")
            .call(d3.axisLeft(yAxis).tickSize(-width).tickFormat('').ticks(10));
        const xAxisValueSecond = secondSvg.append('g')
            .attr('transform', `translate(0, ${height})`)
            .call(d3.axisBottom(xAxis));
        xAxisValueSecond.selectAll(".tick text")
            .attr("rotate", "15")
            .attr("font-family", "cursive")
            .attr("font-size", "14");
        const yAxisValueSecond = secondSvg.append('g')
            .call(d3.axisLeft(yAxis));
        yAxisValueSecond.selectAll(".tick text")
            .attr("rotate", "15")
            .attr("font-family", "cursive")
            .attr("font-size", "14");
        for (const point of secondPoints.filter(point => point.cluster > 1 || secondClusterDetails.Distances.findIndex(clusterPoint => equals(clusterPoint, point)) === details.telemetry[0].SecondIndex)) {
            const index = secondClusterDetails.Distances.findIndex(clusterPoint => equals(clusterPoint, point));
            let color = "#FFFFFF";
            if (index === details.telemetry[0].SecondIndex){
                color = "#000000";
            }
            secondSvg.append("path")
                .attr("d", line([secondCentroid, point]))
                .attr("stroke", color);
        }
        const pointsSvgSecond = secondSvg.append('g')          // place them in a group, so they don't run away
            .attr('id', 'points-svg')              // assign them an id, taking away their individuality
            .selectAll('dot')
            .data(secondPoints)                          // loop over our data
            .join('circle')                        // add a circle
            .attr('cx', d => xAxis(d.x))               // position
            .attr('cy', d => yAxis(d.y))
            .attr('r', 3)                          // radius
            .style('fill', d => color(d.cluster));
        secondSvg.select("#points-svg")
            .selectAll('dot')
            .data(secondPoints)
            .join('text')
            .attr('dx', d => xAxis(d.x) - 2)
            .attr('dy', d => yAxis(d.y) - 5)
            .attr('font-size', '8px')
            .attr('font-family', 'cursive')
            .text(d => secondClusterDetails.Distances.findIndex(point => equals(point, d)));
        const centroidsSvgSecond = secondSvg.append('g')
            .attr('id', 'centroids-svg')
            .selectAll('dot')
            .data([secondCentroid])
            .join('circle')
            .attr('cx', d => xAxis(d.x))
            .attr('cy', d => yAxis(d.y))
            .attr('r', 5)                       // a bit bigger than data points
            .style('fill', '#e6e8ea')           // greyish fill
            .attr('stroke', (d, i) => color(i)) // and a thick colorful outline
            .attr('stroke-width', 3);
        window.addEventListener('resize', function (event) {    // testers hate this one simple function
            d3.selectAll('svg')
                .attr('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`)
                .attr('width', window.innerWidth / 2 - margin.left - margin.right)
                .attr('height', window.innerHeight - margin.top - margin.bottom)
        });
    }

    render() {
        return (<div>
            <div style={{
                display: "flex",
                flexDirection: "row"
            }}>
                <div>
                    <h1>
                        <a href={`/cluster/${this.props.session}/${this.props.firstCluster}`}>
                            {this.props.firstCluster}
                        </a>
                    </h1>
                    <div id={"firstCluster"}></div>
                </div>
                <div>
                    <h1>
                        <a href={`/cluster/${this.props.session}/${this.props.secondCluster}`}>
                            {this.props.secondCluster}
                        </a>
                    </h1>
                    <div id={"secondCluster"}></div>
                </div>
            </div>
            <TableContainer component={Paper} style={{
                background: "rgba(255, 255, 255, 0.25)"
            }}>
                <Table sx={{minWidth: 650}} aria-label="simple table" style={{
                    background: "rgba(255, 255, 255, 0.25)"
                }}>
                    <TableHead>
                        <TableRow>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>Status</TableCell>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>Value type</TableCell>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>First index</TableCell>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>Second index</TableCell>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>First value</TableCell>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>Second value</TableCell>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>Current score</TableCell>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>Max score</TableCell>
                            <TableCell align="center" style={{
                                fontFamily: "roboto",
                                fontSize: "32px"
                            }}>Log statement</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {this.state.details.map((log) => (<TableRow
                            key={log.StatusMessage}
                            sx={{'&:last-child td, &:last-child th': {border: 0}}}
                        >
                            <TableCell align={"center"} component="th" scope="row">
                                {log.IsSuccessful
                                    ? (<FontAwesomeIcon icon={faSquareCheck} style={{
                                        color: "#006930",
                                        fontSize: "36"
                                    }}/>)
                                    : (<FontAwesomeIcon icon={faSquareXmark} style={{
                                        color: "#FF0000",
                                        fontSize: "36"
                                    }}/>)}
                            </TableCell>
                            <TableCell align={"center"} style={{
                                fontFamily: "roboto",
                                fontSize: "20px"
                            }}>{log.ValueType}</TableCell>
                            <TableCell align={"center"} style={{
                                fontFamily: "roboto",
                                fontSize: "20px"
                            }}>{log.FirstIndex ?? ""}</TableCell>
                            <TableCell align={"center"} style={{
                                fontFamily: "roboto",
                                fontSize: "20px"
                            }}>{log.SecondIndex ?? ""}</TableCell>
                            <TableCell align={"center"} style={{
                                fontFamily: "roboto",
                                fontSize: "20px"
                            }}>{!log.FirstValue ? "" : Math.round((log.FirstValue + Number.EPSILON) * 100) / 100}</TableCell>
                            <TableCell align={"center"} style={{
                                fontFamily: "roboto",
                                fontSize: "20px"
                            }}>{!log.SecondValue ? "" : Math.round((log.SecondValue + Number.EPSILON) * 100) / 100}</TableCell>
                            <TableCell align={"center"} style={{
                                fontFamily: "roboto",
                                fontSize: "20px"
                            }}>{log.CurrentScore}</TableCell>
                            <TableCell align={"center"} style={{
                                fontFamily: "roboto",
                                fontSize: "20px"
                            }}>{log.PreviousMaxScore}</TableCell>
                            <TableCell align={"center"} style={{
                                fontFamily: "roboto",
                                fontSize: "20px"
                            }}>{log.StatusMessage}</TableCell>
                        </TableRow>))}
                    </TableBody>
                </Table>
            </TableContainer>
        </div>);
    }
}

export default Wrapper;