export class ClusterDescriptor {
  /**
   * @constructor
   * @param {number[][]} distances - Distances matrix inside cluster
   * @param {number[][]} angles - Angles matrix inside cluster
   */
  constructor(distances, angles) {
    this.distances = distances;
    this.angles = angles;
  }

  /**
   *
   * @returns {number}
   */
  get selectedNode(){
    return this._selectedNode;
  }

  /**
   *
   * @param {number} selectedNode
   */
  set selectedNode(selectedNode){
    this._selectedNode = selectedNode;
  }

  toD3Collection(){
    const clusterCenterId = "Cluster center";
    const indices = [...Array(this.distances[0].length).keys()];
    const nodes = [
      {
        id: clusterCenterId,
        group: 1,
      },
      ...indices.filter(index => index === this.selectedNode).map(index => ({
        id: `X: ${this.distances[0][index]}\nY: ${this.distances[1][index]}\nDistance: ${this.distances[2][index]}`,
        group: 2,
      })),
      ...indices.filter(index => index !== this.selectedNode).map(index => ({
        id: `X: ${this.distances[0][index]}\nY: ${this.distances[1][index]}\nDistance: ${this.distances[2][index]}`,
        group: 3,
      }))
    ];
    const links = [
      ...indices.map(index => ({
          source: clusterCenterId,
          target: `X: ${this.distances[0][index]}\nY: ${this.distances[1][index]}\nDistance: ${this.distances[2][index]}`,
          value: 15
      }))
    ];
    return {
      nodes: nodes,
      links: links
    };
  }
}

export class Step {
  constructor(description, status) {
    this.description = description;
    this.status = status;
  }
}

export class ComparisonStep {
  constructor(descriptor1, descriptor2, steps) {
    this.descriptor1 = descriptor1;
    this.descriptor2 = descriptor2;
    this.steps = steps;
  }
}

export class Comparison {
  constructor(steps) {
    this.steps = steps;
  }
}
