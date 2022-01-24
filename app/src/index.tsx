import React from 'react'
import ReactDOM from 'react-dom'
import App from './App';

const dataSourceUri = document.body.dataset.source;
const root = dataSourceUri
    ? <App dataSourceUri={dataSourceUri} />
    : <p>Data source is not configured.</p>;

ReactDOM.render(
    <React.StrictMode>
        {root}
    </React.StrictMode>,
    document.getElementById('root')
);