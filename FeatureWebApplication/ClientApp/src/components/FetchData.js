import React, { Component } from 'react';

export class FetchData extends Component {
  static displayName = FetchData.name;

  constructor (props) {
    super(props);
    this.state = { features: [], loading: true };

    fetch('api/SampleData/Features')
      .then(response => response.json())
      .then(data => {
        this.setState({ features: data, loading: false });
      });
  }

  static renderFeaturesTable (features) {
    return (
      <table className='table table-striped'>
        <thead>
          <tr>
            <th>Name</th>
            <th>Value</th>
          </tr>
        </thead>
        <tbody>
          {features.map(feature =>
            <tr key={feature.name}>
                <td>{feature.name}</td>
                <td>{feature.value}</td>
            </tr>
          )}
        </tbody>
      </table>
    );
  }

  render () {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
      : FetchData.renderFeaturesTable(this.state.features);

    return (
      <div>
        <h1>Feature list</h1>
        <p>This component demonstrates fetching data from the server.</p>
        {contents}
      </div>
    );
  }
}
