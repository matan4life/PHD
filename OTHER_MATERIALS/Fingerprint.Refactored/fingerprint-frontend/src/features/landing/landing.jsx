import {Box, Paper, Typography} from "@mui/material";
import background from "../../assets/landing-background.jpg";

const Landing = () => {
    return (
        <Box
            sx={{
                backgroundImage: `url(${background})`,
                height: 'calc(100vh - 64px)',
                display: 'flex',
                justifyContent: 'center',
                alignItems: 'center'
            }}
        >
            <Paper elevation={3} sx={{
                width: '25%',
                height: '25%',
                display: 'flex',
                justifyContent: 'center',
                flexDirection: 'column',
                alignItems: 'center'
            }}>
                <Typography component="h2" variant="h6">Welcome to fingerprint recognition system!</Typography>
                <br/>
                <br/>
                <Typography component="h2" variant="subtitle1">Use topbar buttons to make any action in system</Typography>
            </Paper>
        </Box>
    );
};

export default Landing;