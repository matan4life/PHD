import PropTypes from "prop-types";
import {Box, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow} from "@mui/material";

const toTime = dateTimeString => new Date(dateTimeString).toTimeString().split(' ')[0];

const TelemetryTable = ({telemetry}) => {
    return (
        <Box sx={{
            display: 'flex',
            flexDirection: 'column',
            justifyContent: 'center',
            alignItems: 'center',
            height: 'calc(100vh - 120px)',
            rowGap: '30px'
        }}>
            <TableContainer component={Paper}>
                <Table sx={{minWidth: 650}} aria-label="simple table">
                    <TableHead>
                        <TableRow>
                            <TableCell align="right">Step</TableCell>
                            <TableCell align="right">Start Time</TableCell>
                            <TableCell align="right">End Time</TableCell>
                            <TableCell align="right">Duration</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {telemetry.map((record, index) => (
                            <TableRow
                                key={index}
                                sx={{'&:last-child td, &:last-child th': {border: 0}}}
                            >
                                <TableCell component="th" scope="row">
                                    {record.step}
                                </TableCell>
                                <TableCell align="right">{toTime(record.start)}</TableCell>
                                <TableCell align="right">{toTime(record.end)}</TableCell>
                                <TableCell align="right">{record.executionTime}</TableCell>
                            </TableRow>
                        ))}
                    </TableBody>
                </Table>
            </TableContainer>
        </Box>
    );
};

TelemetryTable.propTypes = {
    telemetry: PropTypes.arrayOf(PropTypes.shape({
        step: PropTypes.string.isRequired,
        start: PropTypes.string.isRequired,
        end: PropTypes.string.isRequired,
        executionTime: PropTypes.string.isRequired
    })).isRequired,
};

export default TelemetryTable;