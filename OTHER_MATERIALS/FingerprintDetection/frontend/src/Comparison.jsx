import {Component} from "react";
import * as d3 from "d3";
import {useParams} from "react-router-dom";

const ComparisonWrapper = () => {
    const {session, firstCluster, secondCluster} = useParams();
    return <Comparison session={session} clusterFirst={firstCluster} clusterSecond={secondCluster} />;
}

class Comparison extends Component {
    async componentDidMount() {
        await this.drawTree();
    }

    async drawTree(){
        const session = this.props.session;
        const clusterFirst = this.props.clusterFirst;
        const clusterSecond = this.props.clusterSecond;
        const positionsResponse = await fetch(`http://localhost:5098/sessions/${session}/${clusterFirst}/${clusterSecond}`);
        const positions = await positionsResponse.json();
        console.log(positions);
        const firstIndices = [...new Set(positions.map(x => x.firstPosition))].sort((a, b) => a - b);
        const dataTree = firstIndices.map(x => ({
            name: x,
            children: positions.filter(y => y.firstPosition === x).sort((a, b) => a.secondPosition - b.secondPosition).map(y => ({
                name: y.secondPosition,
                link: `http://localhost:5173/details/${session}/${clusterFirst}/${clusterSecond}/${y.firstPosition}/${y.secondPosition}`
            }))
        }));
        const width = 800;
        const height = 800;
        const cx = width * 0.5; // adjust as needed to fit
        const cy = height * 0.5; // adjust as needed to fit
        const radius = Math.min(width, height) / 2 - 30;

        // Create a radial tree layout. The layout’s first dimension (x)
        // is the angle, while the second (y) is the radius.
        const tree = d3.tree()
            .size([2 * Math.PI, radius])
            .separation((a, b) => (a.parent == b.parent ? 1 : 2) / a.depth);

        // Sort the tree and apply the layout.
        const root = tree(d3.hierarchy({
            name: "Enter",
            children: dataTree
        })
            .sort((a, b) => d3.ascending(a.data.name, b.data.name)));

        d3.select("#radial svg").remove();
        // Creates the SVG container.
        const svg = d3.select("#radial")
            .append("svg")
            .attr("width", width - 30)
            .attr("height", height - 30)
            .attr("viewBox", [-cx, -cy, width, height])
            .attr("style", "width: 100%; height: auto; font: 10px sans-serif;");

        // Append links.
        svg.append("g")
            .attr("fill", "none")
            .attr("stroke", "#555")
            .attr("stroke-opacity", 0.4)
            .attr("stroke-width", 1.5)
            .selectAll()
            .data(root.links())
            .join("path")
            .attr("d", d3.linkRadial()
                .angle(d => d.x)
                .radius(d => d.y));

        // Append nodes.
        svg.append("g")
            .selectAll()
            .data(root.descendants())
            .join("circle")
            .attr("transform", d => `rotate(${d.x * 180 / Math.PI - 90}) translate(${d.y},0)`)
            .attr("fill", d => d.children ? "#555" : "#999")
            .attr("r", 2.5);

        // Append labels.
        svg.append("g")
            .attr("stroke-linejoin", "round")
            .attr("stroke-width", 3)
            .selectAll()
            .data(root.descendants().filter(x => !x.data.link))
            .join("text")
            .attr("transform", d => `rotate(${d.x * 180 / Math.PI - 90}) translate(${d.y},0) rotate(${d.x >= Math.PI ? 180 : 0})`)
            .attr("dy", "0.31em")
            .attr("x", d => d.x < Math.PI === !d.children ? 6 : -6)
            .attr("text-anchor", d => d.x < Math.PI === !d.children ? "start" : "end")
            .attr("paint-order", "stroke")
            .attr("stroke", "white")
            .attr("fill", "currentColor")
            .attr("font-family", "cursive")
            .text(d => d.data.name);

        svg.append("g")
            .attr("stroke-linejoin", "round")
            .attr("stroke-width", 3)
            .selectAll()
            .data(root.descendants().filter(x => x.data.link))
            .join("a")
            .attr("href", d => d.data.link)
            .append("text")
            .attr("transform", d => `rotate(${d.x * 180 / Math.PI - 90}) translate(${d.y},0) rotate(${d.x >= Math.PI ? 180 : 0})`)
            .attr("dy", "0.31em")
            .attr("x", d => d.x < Math.PI === !d.children ? 6 : -6)
            .attr("text-anchor", d => d.x < Math.PI === !d.children ? "start" : "end")
            .attr("paint-order", "stroke")
            .attr("stroke", "white")
            .attr("fill", "currentColor")
            .attr("font-family", "cursive")
            .text(d => d.data.name);
    }

    render(){
        return <div id={"radial"}></div>
    }
}

export default ComparisonWrapper;