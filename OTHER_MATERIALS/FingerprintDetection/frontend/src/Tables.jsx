import {Component} from "react";
import * as d3 from "d3";

class Tables extends Component {
    async componentDidMount() {
        await this.renderTables();
    }

    async renderTables(){
        const response = await fetch(`http://localhost:5098/descriptors/${this.props.session}/${this.props.cluster}`);
        const data = await response.json();
        const indices = Array(data.Distances.length).keys();
        const distanceHeaders = ["", ...indices];
        const distanceRowStarts = ["X", "Y", "Distance"];
        const distanceData = data.Distances[0].map((_, colIndex) => data.Distances.map(row => row[colIndex]));
        for (let i = 0; i < distanceData.length; i++){
            for (let j = 0; j < distanceData[i].length; j++){
                distanceData[i][j] = Math.round((distanceData[i][j] + Number.EPSILON) * 100) / 100
            }
        }
        const renderDistanceData = distanceData.map((x, rowIndex) =>[distanceRowStarts[rowIndex], ...x]);
        const angleIndices = Array(data.Angles.length).keys();
        const angleRowStarts = [...angleIndices];
        const angleHeaders = ["", ...angleRowStarts];
        console.log(renderDistanceData);
        for (let i = 0; i < data.Angles.length; i++){
            for (let j = 0; j < data.Angles[i].length; j++){
                data.Angles[i][j] = Math.round((data.Angles[i][j] + Number.EPSILON) * 100) / 100
            }
        }
        const angleData = data.Angles.map((x, rowIndex) => [angleRowStarts[rowIndex], ...x]);
        d3.select("#distances table").remove();
        const distances = d3.select("#distances")
            .append("table")
            .style("border-collapse", "collapse")
            .style("border", "2px black solid");

        distances.append("thead").append("tr")
            .selectAll("th")
            .data(distanceHeaders)
            .enter().append("th")
            .text(function(d) { return d; })
            .style("border", "1px black solid")
            .style("background-color", "lightgray")
            .style("font-weight", "bold")
            .style("padding", "12px")
            .style("text-align", "center")
            .style("font-size", "36px")
            .style("font-family", "cursive")
            .style("text-transform", "uppercase");
        // data
        distances.append("tbody")
            .selectAll("tr").data(renderDistanceData)
            .enter().append("tr")
            .selectAll("td")
            .data(function(d){return d;})
            .enter().append("td")
            .style("border", "1px black solid")
            .style("padding", "12px")
            .style("text-align", "center")
            .on("mouseover", function(){
                d3.select(this).style("background-color", "powderblue");
            })
            .on("mouseout", function(){
                d3.select(this).style("background", "rgba(255, 255, 255, 0)");
            })
            .text(function(d){return d;})
            .style("font-family", "cursive")
            .style("font-size", "36px");

        d3.select("#angles table").remove();
        const angles = d3.select("#angles")
            .append("table")
            .style("border-collapse", "collapse")
            .style("border", "2px black solid");

        angles.append("thead").append("tr")
            .selectAll("th")
            .data(angleHeaders)
            .enter().append("th")
            .text(function(d) { return d; })
            .style("border", "1px black solid")
            .style("background-color", "lightgray")
            .style("font-weight", "bold")
            .style("font-size", "36px")
            .style("font-family", "cursive")
            .style("padding", "12px")
            .style("text-align", "center")
            .style("text-transform", "uppercase");

        // data
        angles.append("tbody")
            .selectAll("tr")
            .data(angleData)
            .enter()
            .append("tr")
            .selectAll("td")
            .data(function(d){return d;})
            .enter().append("td")
            .style("border", "1px black solid")
            .style("padding", "12px")
            .style("text-align", "center")
            .on("mouseover", function(){
                d3.select(this).style("background-color", "powderblue");
            })
            .on("mouseout", function(){
                d3.select(this).style("background", "rgba(255, 255, 255, 0)");
            })
            .text(function(d){return d;})
            .style("font-family", "cursive")
            .style("font-size", "36px");
    }

    render() {
        return <>
            <h1 style={{
                textAlign: "center"
            }}>Angles (in degrees)</h1>
            <div id={'angles'} style={{
                display: "flex",
                justifyContent: "center"
            }}></div>
            <h1 style={{
                textAlign: "center"
            }}>Distances</h1>
            <div id={'distances'} style={{
                display: "flex",
                justifyContent: "center"
            }}></div>
        </>
    }
}

export default Tables;