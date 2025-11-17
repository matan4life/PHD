import {ClusterDescriptor} from "./models.js";

const testData

const appendDescriptor = (descriptor, container) => {
  const width = 600, height = 600, color = d3.scaleOrdinal(d3.schemePaired);
  const testGroups = descriptor.toD3Collection();
  const links = testGroups.links.map(x => ({...x}));
  const nodes = testGroups.nodes.map(x => ({...x}));
  const simulation = d3.forceSimulation(nodes)
    .force("link", d3.forceLink(links).id(d => d.id))
    .force("charge", d3.forceManyBody())
    .force("center", d3.forceCenter(width / 2, height / 2))
    .on("tick", ticked);
// Create the SVG container.
  const svg = d3.create("svg")
    .attr("width", width)
    .attr("height", height)
    .attr("viewBox", [0, 0, width, height])
    .attr("style", "max-width: 100%; height: auto; border-style: dotted; background-image: linear-gradient(to right, rgba(46, 149, 146, 1), rgba(27, 255, 255, 1));");
// Add a line for each link, and a circle for each node.
  const link = svg.append("g")
    .attr("stroke", "#999")
    .attr("stroke-opacity", 0.6)
    .selectAll()
    .data(links)
    .join("line")
    .attr("stroke-width", d => Math.sqrt(d.value));
  const node = svg.append("g")
    .attr("stroke", "#fff")
    .attr("stroke-width", 1.5)
    .selectAll()
    .data(nodes)
    .join("circle")
    .attr("r", 10)
    .attr("fill", d => color(d.group));
  node.append("title")
    .text(d => d.id);

  node.call(d3.drag()
    .on("start", dragstarted)
    .on("drag", dragged)
    .on("end", dragended));

// Set the position attributes of links and nodes each time the simulation ticks.
  function ticked() {
    link
      .attr("x1", d => d.source.x)
      .attr("y1", d => d.source.y)
      .attr("x2", d => d.target.x)
      .attr("y2", d => d.target.y);

    node
      .attr("cx", d => d.x)
      .attr("cy", d => d.y);
  }

// Reheat the simulation when drag starts, and fix the subject position.
  function dragstarted(event) {
    if (!event.active) simulation.alphaTarget(0.3).restart();
    event.subject.fx = event.subject.x;
    event.subject.fy = event.subject.y;
  }

// Update the subject (dragged node) position during drag.
  function dragged(event) {
    event.subject.fx = event.x;
    event.subject.fy = event.y;
  }

// Restore the target alpha so the simulation cools after dragging ends.
// Unfix the subject position now that it’s no longer being dragged.
  function dragended(event) {
    if (!event.active) simulation.alphaTarget(0);
    event.subject.fx = null;
    event.subject.fy = null;
  }

  container.append(svg.node());
};

/**
 *
 * @param {number[][]} table
 * @param {string[]} rowHeaders
 * @param {string[]} columnHeaders
 * @param {string} container
 */
const appendTable = (table, rowHeaders, columnHeaders, container) => {
  const domElement = document.getElementById(container);
  const domTable = document.createElement("table");
  domTable.className = "styled-table";
  const header = domTable.createTHead();
  for (let index of columnHeaders){
    const headerCell = document.createElement("th");
    headerCell.textContent = index.toString();
    header.appendChild(headerCell);
  }
  for (let row = 0; row < table.length; row++){
    const tableRow = domTable.insertRow();
    tableRow.insertCell()
      .appendChild(document.createTextNode(rowHeaders[row]));
    for (let column = 0; column < table[0].length; column++){
      const tableDefinition = tableRow.insertCell();
      tableDefinition.appendChild(document.createTextNode(table[row][column].toString()));
    }
  }
  domElement.appendChild(domTable);
}

appendDescriptor(descriptorTest, first_cluster_container);
// appendDescriptor(otherDescriptorTest, other_container);
appendTable(descriptorTest.distances, ["X", "Y", "Distance"], ["/", ...Array(descriptorTest.distances[0].length).keys()], "first_cluster_distance_table");
appendTable(descriptorTest.angles, [...Array(descriptorTest.distances[0].length).keys()], ["/", ...Array(descriptorTest.distances[0].length).keys()], "first_cluster_angle_table");
