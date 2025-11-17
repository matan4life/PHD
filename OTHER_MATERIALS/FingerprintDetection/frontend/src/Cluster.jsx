import {Component} from "react";
import * as d3 from "d3";

class Cluster extends Component {
    async componentDidMount() {
        await this.drawClusters();
    }

    async drawClusters() {
        const response = await fetch(`http://localhost:5098/descriptors/${this.props.session}/${this.props.cluster}`);
        const data = await response.json();
        const margin = {top: 30, right: 30, bottom: 30, left: 50};
        const viewBox = {x: 0, y: 0, w: 500, h: 500};
        const width = viewBox.w - margin.left - margin.right;
        const height = viewBox.h - margin.top - margin.bottom;
        d3.select("#container svg").remove();
        const svg = d3.select('#container')
            .append('svg')
            .attr('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`)
            .attr('width', window.innerWidth - margin.left - margin.right)
            .attr('height', window.innerHeight - margin.top - margin.bottom)
            .append('g')
            .attr('transform', `translate(${margin.left}, ${margin.top})`)     // mind the margins
            .attr('color', '#000000')
            .attr('font-weight', 'bold')                                       // we are bold enough to do this
            .attr('stroke-width', 2);
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
        svg.append('g')
            .attr("class", "x axis-grid")
            .attr('transform', 'translate(0,' + height + ')')
            .call(d3.axisBottom(xAxis).tickSize(-height).tickFormat('').ticks(10));
        svg.append('g')
            .attr("class", "y axis-grid")
            .call(d3.axisLeft(yAxis).tickSize(-width).tickFormat('').ticks(10));
        const xAxisValue = svg.append('g')
            .attr('transform', `translate(0, ${height})`)
            .call(d3.axisBottom(xAxis));
        xAxisValue.selectAll(".tick text")
            .attr("rotate", "15")
            .attr("font-family", "cursive")
            .attr("font-size", "14");
        const yAxisValue = svg.append('g')
            .call(d3.axisLeft(yAxis));
        yAxisValue.selectAll(".tick text")
            .attr("rotate", "15")
            .attr("font-family", "cursive")
            .attr("font-size", "14");
        const centroid = {
            x: data.Center.X, y: xRange[1] - data.Center.Y, cluster: 0
        };
        const points = data.Distances.map((point, index) => ({
            x: point[0], y: xRange[1] - point[1], cluster: 1
        }));
        const centroids = [centroid];
        const line = d3.line((d) => xAxis(d.x), d => yAxis(d.y));
        for (const point of points) {
            svg.append("path")
                .attr("d", line([centroid, point]))
                .attr("stroke", "#FFFFFF");
        }
        const pointsSvg = svg.append('g')          // place them in a group, so they don't run away
            .attr('id', 'points-svg')              // assign them an id, taking away their individuality
            .selectAll('dot')
            .data(points)                          // loop over our data
            .join('circle')                        // add a circle
            .attr('cx', d => xAxis(d.x))               // position
            .attr('cy', d => yAxis(d.y))
            .attr('r', 3)                          // radius
            .style('fill', d => color(d.cluster));
        const equals = (point, d) => {
            return point[0] === d.x && d.y === xRange[1] - point[1];
        }
        svg.select("#points-svg")
            .selectAll('dot')
            .data(points)
            .join('text')
            .attr('dx', d => xAxis(d.x) - 2)
            .attr('dy', d => yAxis(d.y) - 5)
            .attr('font-size', '8px')
            .attr('font-family', 'cursive')
            .text(d => data.Distances.findIndex(point => equals(point, d)));
        const centroidsSvg = svg.append('g')
            .attr('id', 'centroids-svg')
            .selectAll('dot')
            .data(centroids)
            .join('circle')
            .attr('cx', d => xAxis(d.x))
            .attr('cy', d => yAxis(d.y))
            .attr('r', 5)                       // a bit bigger than data points
            .style('fill', '#e6e8ea')           // greyish fill
            .attr('stroke', (d, i) => color(i)) // and a thick colorful outline
            .attr('stroke-width', 3);
        window.addEventListener('resize', function (event) {    // testers hate this one simple function
            d3.select('svg')
                .attr('viewBox', `${viewBox.x} ${viewBox.y} ${viewBox.w} ${viewBox.h}`)
                .attr('width', window.innerWidth - margin.left - margin.right)
                .attr('height', window.innerHeight - margin.top - margin.bottom)
        });
    }

    render() {
        return <>
            <h1 style={{
                textAlign: "center"
            }}>Cluster visualization</h1>
            <div id={"container"} className={"test"}></div>
        </>
    }
}

export default Cluster;