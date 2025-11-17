import React from 'react'
import ReactDOM from 'react-dom/client'
import './index.css'
import {BrowserRouter, Route, Routes} from "react-router-dom";
import ClusterInfo from "./ClusterInfo.jsx";
import Wrapper from "./ComparisonDetails.jsx";
import Session from "./Session.jsx";
import ComparisonWrapper from "./Comparison.jsx";
import App from "./App.jsx";

ReactDOM.createRoot(document.getElementById('root')).render(
    <React.StrictMode>
        <BrowserRouter>
            <Routes>
                <Route exact path={'/cluster/:session/:cluster'} element={<ClusterInfo />} />
                <Route exact
                       path={'/details/:session/:firstCluster/:secondCluster/:firstIndex/:secondIndex'}
                       element={<Wrapper />} />
                <Route exact
                       path={'/session/:session'}
                       element={<Session />}
                       />
                <Route exact path={'/comparison/:session/:firstCluster/:secondCluster'}
                       element={<ComparisonWrapper />} />
                <Route exact path={'/'}
                       element={<App />} />
            </Routes>
        </BrowserRouter>
    </React.StrictMode>,
)
