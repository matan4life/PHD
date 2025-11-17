import {useState} from "react";
import Steps from "../stepper/stepper.jsx";
import CreateDataset from "../createDataset/createDataset.jsx";
import LoadingSpinner from "../loadingSpinner/loadingSpinner.jsx";
import TelemetryTable from "../telemetryTable/telemetryTable.jsx";
import {Box, Button} from "@mui/material";
import {
    useAnalyzeImagesMutation,
    useCalculateClustersMutation,
    useCalculateMetricsMutation,
    useCompareDatasetsMutation,
    useGetAvailableDatasetsQuery,
    useRegisterDatasetMutation,
    useUploadDatasetToServerMutation
} from "../../redux/api.js";

const ProcessDataset = () => {
    const initialStepsConfig = [
        {
            name: "Select dataset name",
            isOptional: false,
            isSkipped: false
        },
        {
            name: "Send images to server",
            isOptional: true,
            isSkipped: false
        },
        {
            name: "Creating dataset",
            isOptional: false,
            isSkipped: false
        },
        {
            name: "Preprocessing images",
            isOptional: false,
            isSkipped: false
        },
        {
            name: "Extracting clusters",
            isOptional: false,
            isSkipped: false
        },
        {
            name: "Calculating metrics",
            isOptional: false,
            isSkipped: false
        },
        {
            name: "Comparing clusters",
            isOptional: false,
            isSkipped: false
        }
    ];

    const [step, setStep] = useState(0);
    const [datasetName, setDatasetName] = useState('');
    const [newDatasetName, setNewDatasetName] = useState('');
    const [files, setFiles] = useState([]);
    const [telemetry, setTelemetry] = useState([]);
    const [steps, setSteps] = useState([...initialStepsConfig]);
    const [testRunId, setTestRunId] = useState(0);
    const {data: availableDatasets, isLoading: isLoadingDatasets} = useGetAvailableDatasetsQuery();
    const [uploadNewDataset, {isLoading: isUploadingNewDataset}] = useUploadDatasetToServerMutation();
    const [registerDataset, {isLoading: isRegisteringDataset}] = useRegisterDatasetMutation();
    const [analyseImages, {isLoading: isAnalysingImages}] = useAnalyzeImagesMutation();
    const [calculateClusters, {isLoading: isCalculatingClusters}] = useCalculateClustersMutation();
    const [calculateMetrics, {isLoading: isCalculatingMetrics}] = useCalculateMetricsMutation();
    const [compareDatasets, {isLoading: isComparingDatasets}] = useCompareDatasetsMutation();

    const helperMessages = [
        "",
        "Sending...",
        "Creating dataset...",
        "Processing images...",
        "Extracting clusters...",
        "Calculating metrics...",
        "Comparing clusters (take patience, it could take up to 10 mins)",
    ];

    const actions = [
        registerDataset,
        analyseImages,
        calculateClusters,
        calculateMetrics,
        compareDatasets
    ]

    const loadingStatuses = [
        isLoadingDatasets,
        isUploadingNewDataset,
        isRegisteringDataset,
        isAnalysingImages,
        isCalculatingClusters,
        isCalculatingMetrics,
        isComparingDatasets
    ];

    const submit = () => {
        if (step === 0) {
            if (!datasetName && newDatasetName) {
                submitNewDataset();
            } else {
                const newSteps = [...steps];
                newSteps.find(s => s.isOptional).isSkipped = true;
                setSteps([...newSteps]);
                setStep(2);
            }
        } else {
            const action = actions[step - 2];
            if (step === 2) {
                const actualDatasetName = datasetName || newDatasetName;
                action(actualDatasetName)
                    .unwrap()
                    .then(response => {
                        setTestRunId(response.testRunId);
                        updateTelemetry(response.telemetry);
                        setStep(step + 1);
                    });
            } else {
                action(testRunId)
                    .unwrap()
                    .then(response => {
                        updateTelemetry(response);
                        setStep(step + 1);
                    });
            }
        }
    }

    const submitNewDataset = () => {
        setStep(1);
        const formData = new FormData();
        formData.append("datasetName", newDatasetName);
        for (const file of files) {
            formData.append("files", file);
        }
        uploadNewDataset(formData)
            .unwrap()
            .then(response => {
                updateTelemetry(response);
                setStep(2);
            });
    }

    const updateTelemetry = (telemetryResponse) => {
        setTelemetry([...telemetry, {
            step: steps[step].name,
            start: telemetryResponse.start,
            end: telemetryResponse.end,
            executionTime: telemetryResponse.executionTime
        }]);
    };

    return (
        <div style={{
            display: 'flex',
            flexFlow: 'column',
            height: '100%',
            rowGap: '10px',
        }}>
            <Steps activeStep={step} steps={steps}/>
            {
                loadingStatuses.some(status => status)
                    ? <LoadingSpinner spinnerMessage={helperMessages[step]}/>
                    : step === 0
                        ? <CreateDataset setNewDatasetName={setNewDatasetName}
                                         submit={submit}
                                         setDatasetName={setDatasetName}
                                         datasetName={datasetName}
                                         availableDatasets={availableDatasets}
                                         setFiles={setFiles}/>
                        : step === 7
                            ? <TelemetryTable telemetry={telemetry}/>
                            : <Box sx={{
                                display: 'flex',
                                flexDirection: 'column',
                                justifyContent: 'center',
                                alignItems: 'center',
                                height: 'calc(100vh - 105px)',
                                rowGap: '30px'
                            }}>
                                <Button variant="contained" onClick={submit}>Continue</Button>
                            </Box>
            }
        </div>
    );
};

export default ProcessDataset;