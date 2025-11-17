import {Box, Button, FormControl, Input, InputLabel, MenuItem, Select, TextField, Typography} from "@mui/material";
import PropTypes from "prop-types";

const CreateDataset = ({
                           datasetName,
                           setDatasetName,
                           setNewDatasetName,
                           setFiles,
                           availableDatasets,
                           submit
                       }) => {
    return (
        <Box sx={{
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
            height: 'calc(100vh - 120px)',
            rowGap: '30px'
        }}>
            <Typography component={'h1'} variant="h5">
                Enter your dataset name and select necessary files
            </Typography>
            <Box sx={{
                display: 'flex',
                flexDirection: 'row',
                justifyContent: 'center',
                alignItems: 'center',
                columnGap: '50px'
            }}>
                <TextField variant={'outlined'}
                           label={'Dataset name'}
                           onChange={(e) => setNewDatasetName(e.target.value)}
                           disabled={datasetName !== ''}
                />
                <Input type={'file'}
                       disabled={datasetName !== ''}
                       onChange={(e) => setFiles(e.target.files)}
                       inputProps={{
                           accept: 'image/*',
                           multiple: true,

                       }}/>
            </Box>
            {availableDatasets.length > 0
                ? (
                    <>
                        <Typography component={'h1'} variant="h5">
                            ...or use existing dataset
                        </Typography>
                        <Box sx={{minWidth: 125}}>
                            <FormControl fullWidth>
                                <InputLabel id="demo-simple-select-label">Dataset name</InputLabel>
                                <Select
                                    labelId="demo-simple-select-label"
                                    id="demo-simple-select"
                                    value={datasetName}
                                    label="Dataset name"
                                    onChange={(e) => setDatasetName(e.target.value)}
                                >
                                    {availableDatasets.map((dataset) => (
                                        <MenuItem key={dataset} value={dataset}>{dataset}</MenuItem>))}
                                </Select>
                            </FormControl>
                        </Box>
                    </>
                )
                : null
            }
            <Button onClick={(e) => submit()}
                    variant="contained">Send dataset data</Button>
        </Box>
    );
};

CreateDataset.propTypes = {
    datasetName: PropTypes.string.isRequired,
    setDatasetName: PropTypes.func.isRequired,
    setNewDatasetName: PropTypes.func.isRequired,
    setFiles: PropTypes.func.isRequired,
    availableDatasets: PropTypes.arrayOf(PropTypes.string).isRequired,
    submit: PropTypes.func.isRequired
};

export default CreateDataset;