import React, { useState } from 'react';
import Uploader from './uploader/Uploader';
import Gallery from './gallery/Gallery';
import { BrowserRouter as Router, Switch, Route, Link } from 'react-router-dom';
import Header from './Header';
import FilesDragAndDrop from './components/FilesDragAndDrop';

export default function App() {
  const [refresh, setRefresh] = useState(false);
  const [showUploader, toggleUploader] = useState(true);
  const [files, setFiles] = useState([])

  const handleDrop = (files) => {
    // signal the uploader with dropped files
    setFiles(files);
  }

  // signal the gallery to refresh
  const handleUploaded = () => setRefresh(!refresh);

  const handleUploaderClose = () => toggleUploader(false);

  return (
    <div style={{ width: '100%', height: '100%', position: 'relative' }}>
      <FilesDragAndDrop onDrop={handleDrop}>
        <Router>
          <Header />
          <Switch>
            <Route path="/">
              <Gallery refresh={refresh} />
            </Route>
          </Switch>
        </Router>
        {showUploader && (
          <Uploader
            files={files}
            onUploaded={handleUploaded}
            onClose={handleUploaderClose}
          />
        )}
      </FilesDragAndDrop>
    </div>
  );
}
