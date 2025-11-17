import {BrowserRouter, Route, Routes} from "react-router-dom";
import ProcessDataset from "./features/processDataset/processDataset.jsx";
import Menu from "./features/menu/menu.jsx";
import AnalyticsComponent from "./features/analytics/analyticsComponent.jsx";

function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path='/' element={<Menu />}>
                    <Route index element={<ProcessDataset />} />
                    <Route path={'analytics'} element={<AnalyticsComponent />} />
                </Route>
            </Routes>
        </BrowserRouter>
    );
}

export default App
