import {Box, CircularProgress, Typography} from "@mui/material";
import PropTypes from "prop-types";

const LoadingSpinner = ({spinnerMessage}) => {
    return (<Box sx={{
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
            rowGap: '50px',
            height: 'calc(100vh - 105px)'
        }}>
            <CircularProgress/>
            <Typography component={'h1'} variant="h6">{spinnerMessage}</Typography>
        </Box>);
};

LoadingSpinner.propTypes = {
    spinnerMessage: PropTypes.string.isRequired,
};

export default LoadingSpinner;