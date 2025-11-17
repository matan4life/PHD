import * as d3 from "d3";

class ClusterPlot {
    constructor(outerElement) {
        this.D3 = outerElement.append('svg');
    }

    setupAxis(x, y) {
        const xRange = [0, x];
        const yRange = [y, 0];
        const xAxis = d3.scaleLinear()
            .domain(xRange)
            .range(xRange);
        const yAxis = d3.scaleLinear()
            .domain(yRange)
            .range(yRange);
        this.D3
            .attr('viewBox', `0 0 ${x} ${y}`)
            .attr('width', x)
            .attr('height', y)
        this.D3.append('g')
            .attr("class", "x axis-grid")
            .attr('transform', 'translate(0,' + y + ')')
            .call(d3.axisBottom(xAxis).tickSize(-y).tickFormat('').ticks(10));
        this.D3.append('g')
            .attr("class", "y axis-grid")
            .call(d3.axisLeft(yAxis).tickSize(-x).tickFormat('').ticks(10));
        const xAxisValueFirst = this.D3.append('g')
            .attr('transform', `translate(0, ${y})`)
            .call(d3.axisBottom(xAxis));
        xAxisValueFirst.selectAll(".tick text")
            .attr("rotate", "15")
            .attr("font-family", "cursive")
            .attr("font-size", "14");
        const yAxisValueFirst = this.D3.append('g')
            .call(d3.axisLeft(yAxis));
        yAxisValueFirst.selectAll(".tick text")
            .attr("rotate", "15")
            .attr("font-family", "cursive")
            .attr("font-size", "14");
    }
}

export default ClusterPlot;