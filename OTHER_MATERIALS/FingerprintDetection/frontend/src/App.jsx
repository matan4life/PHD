import {useEffect, useState} from "react";
import {Chip, ListItem, Paper} from "@mui/material";

const App = () => {
    const [sessions, setSessions] = useState([]);
    useEffect(() => {
        const fetchSessions = async() => {
            const response = await fetch("http://localhost:5098/sessions");
            const sessions = await response.json();
            setSessions(sessions);
        }
        fetchSessions();
    }, []);

    return <>
        <h1 style={{
            margin: 0
        }}>Available sessions</h1>
        <Paper
            sx={{
                display: 'flex',
                flexWrap: "wrap",
                background: 'rgba(255, 255, 255, 0)',
                justifyContent: 'center',
                margin: 0
            }}
            component="ul"
        >
            {sessions.map((session) => {
                return (
                    <ListItem key={session.sessionId}>
                        <Chip
                            label={`${session.sessionId} - ${session.sessionCreated}`}
                            component="a"
                            href={`/session/${session.sessionId}`}
                            variant="outlined"
                            clickable
                        />
                    </ListItem>
                );
            })}
        </Paper>
    </>
};

export default App;