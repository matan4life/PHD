import PropTypes from "prop-types";
import {Box, Step, StepLabel, Stepper, Typography} from "@mui/material";

const Steps = ({steps, activeStep}) => {
    return (<Box sx={{
        flex: '0 1 auto',
        justifyContent: "center",
        alignItems: "center"
    }}>
        <Stepper activeStep={activeStep}>
            {steps.map((step, index) => {
                let stepProps = {};
                let labelProps = {};
                if (step.isOptional) {
                    labelProps = {
                        ...labelProps, optional: (<Typography variant="caption">Optional</Typography>)
                    };
                }
                if (step.isSkipped) {
                    stepProps = {...stepProps, completed: false};
                }
                return (<Step key={index} {...stepProps}>
                    <StepLabel {...labelProps}>{step.name}</StepLabel>
                </Step>)
            })}
        </Stepper>
    </Box>);
};

Steps.propTypes = {
    steps: PropTypes.arrayOf(PropTypes.shape({
        name: PropTypes.string.isRequired,
        isOptional: PropTypes.bool.isRequired,
        isSkipped: PropTypes.bool.isRequired,
    })).isRequired,
    activeStep: PropTypes.number.isRequired,
};

export default Steps;