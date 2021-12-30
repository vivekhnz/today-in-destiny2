import React from 'react'
import ReactDOM from 'react-dom'
import App from './App';

const dataSourceUri = document.body.dataset.source;
if (dataSourceUri) {
    ReactDOM.render(
        <App dataSourceUri={dataSourceUri} />,
        document.getElementById('root')
    );
}
else {
    ReactDOM.render(
        <p>Data source is not configured.</p>,
        document.getElementById('root')
    );
}