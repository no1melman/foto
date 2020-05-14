import React, { useState } from 'react';
import Uploader from './uploader/Uploader';
import Gallery from './gallery/Gallery';

export default function App() {
  const [refresh, setRefresh] = useState(false);

  const handleUploaded = () => setRefresh(!refresh)

  return (
    <div>
      <Gallery refresh={refresh} />

      <Uploader onUploaded={handleUploaded} />
    </div>
  );
}
