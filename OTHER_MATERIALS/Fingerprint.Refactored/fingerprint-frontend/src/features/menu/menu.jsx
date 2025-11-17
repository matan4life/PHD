import {AppBar, Box, FormControl, IconButton, InputLabel, MenuItem, Select, Toolbar, Typography} from "@mui/material";
import {Fingerprint} from "@mui/icons-material";
import {Outlet} from "react-router-dom";
import {useGetTestRunsQuery} from "../../redux/api.js";
import {useDispatch, useSelector} from "react-redux";
import {useState} from "react";
import {setTestRun} from "../../redux/testRunSlice.js";

const Menu = () => {
    const { data: testRuns } = useGetTestRunsQuery();
    const dispatch = useDispatch();
    const [selectedTestRun, setSelectedTestRun] = useState(null);
    const { testRun: stateTestRun } = useSelector((state) => state.testRun);

    const updateTestRun = (id) => {
        const selectedTestRun = testRuns.find(testRun => testRun.id === id);
        setSelectedTestRun(selectedTestRun);
        dispatch(setTestRun(selectedTestRun));
        console.log(stateTestRun);
    }

    const getTestRunDisplayName = (testRun) => {
        const dateTime = new Date(testRun.startDate);
        return `${testRun.datasetPath.split('\\').pop()} - ${dateTime.toLocaleString()}`;
    };

    return (<>
        <Box sx={{flexGrow: 1 }}>
            <AppBar position="static" sx={{
                opacity: 1
            }}>
                <Toolbar>
                    <IconButton
                        size="large"
                        edge="start"
                        color="inherit"
                        aria-label="menu"
                        sx={{mr: 2}}
                    >
                        <Fingerprint/>
                    </IconButton>
                    <Typography variant="h6" component="div" sx={{flexGrow: 5}}>
                        Process new dataset
                    </Typography>
                    <Typography variant="h6" component="div" sx={{flexGrow: 5}}>
                        <a href={'/analytics'}>View results</a>
                    </Typography>
                    {
                        !stateTestRun
                            ? null
                            : <Typography variant={'h6'} component={'div'} sx={{flexGrow: 5}}>Selected dataset: {getTestRunDisplayName(stateTestRun)}</Typography>
                    }
                    {
                        !testRuns
                            ? null
                            : <Box sx={{minWidth: 125, flexGrow: 5, backgroundColor: '#FFFFFF'}}>
                                <FormControl variant={'filled'} fullWidth>
                                    <InputLabel id="demo-simple-select-label">Selected dataset name</InputLabel>
                                    <Select
                                        labelId="demo-simple-select-label"
                                        id="demo-simple-select"
                                        value={selectedTestRun?.id ?? 0}
                                        label="Dataset name"
                                        onChange={(e) => updateTestRun(e.target.value)}
                                    >
                                        {testRuns.map((testRun) => (
                                            <MenuItem key={testRun.id} value={testRun.id}>{getTestRunDisplayName(testRun)}</MenuItem>))}
                                    </Select>
                                </FormControl>
                            </Box>
                    }
                </Toolbar>
            </AppBar>
        </Box>
        <Outlet/>
    </>);
};

export default Menu;