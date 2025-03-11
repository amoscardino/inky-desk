wall = 4;
base_width = wall * 2;
height_back = 45;
height_front = 5;
depth = 85;
depth_support = 60;
width = 65;

$fn = 12;

module base() {
    translate([base_width, 0, 0]) {
        difference() {
            cube([width - (base_width * 2), base_width, wall * 2]);
            translate([base_width, wall, -0.1])
                cube([(width / 2) + 0.5, wall * 2, wall * 2 + 0.2]);
        }
    }

    translate([0, base_width, 0])
        cube([base_width, depth - (base_width * 2), wall * 2]);

    translate([width - base_width, base_width, 0])
        cube([base_width, depth - (base_width * 2), wall * 2]);

    translate([base_width, depth - base_width, 0])
        cube([width - (base_width * 2), base_width, wall * 2]);

    translate([base_width, base_width, 0]) {
        difference() {
            cylinder(wall * 2, base_width, base_width, center = false);
            translate([0, 0, -0.1])
                cube([base_width, base_width, wall * 2 + 0.2]);
        }
    }

    translate([width - base_width, base_width, 0]) {
        difference() {
            cylinder(wall * 2, base_width, base_width, center = false);
            translate([base_width * -1, 0, -0.1])
                cube([base_width, base_width, wall * 2 + 0.2]);
        }
    }

    translate([base_width, depth - base_width, 0]) {
        difference() {
            cylinder(wall * 2, base_width, base_width, center = false);
            translate([0, base_width * -1, -0.1])
                cube([base_width, base_width, wall * 2 + 0.2]);
        }
    }

    translate([width - base_width, depth - base_width, 0]) {
        difference() {
            cylinder(wall * 2, base_width, base_width, center = false);
            translate([base_width * -1, base_width * -1, -0.1])
                cube([base_width, base_width, wall * 2 + 0.2]);
        }
    }
}

module front() {
    translate([base_width, 0, wall * 2 - 0.01])
        cube([base_width, base_width, height_front]);
    
    translate([width - (base_width * 2), 0, wall * 2 - 0.01])
        cube([base_width, base_width, height_front]);
}

module support() {
    CubePoints = [
        [  0,  depth - (base_width * 1.5) + wall,  0 ],
        [ wall,  depth - (base_width * 1.5)  + wall,  0 ],
        [ wall,  depth - (base_width * 1.5),  0 ],
        [  0,  depth - (base_width * 1.5),  0 ],
        [  0,  depth_support,  height_back],
        [ wall,  depth_support,  height_back],
        [ wall,  depth_support - wall,  height_back],
        [  0,  depth_support - wall,  height_back],
    ];
  
    CubeFaces = [
        [0,1,2,3], // bottom
        [4,5,1,0], // front
        [7,6,5,4], // top
        [5,6,2,1], // right
        [6,7,3,2], // back
        [7,4,0,3], // left
    ]; 
  
    translate([0, 0, wall * 2 - 0.1]) {
        polyhedron(CubePoints, CubeFaces);
        
        translate([width - wall, 0, 0])
            polyhedron(CubePoints, CubeFaces);
        
        translate([0, (depth_support - wall), 0])
            cube([wall, wall, height_back]);
        
        translate([width - wall, (depth_support - wall), 0])
            cube([wall, wall, height_back]);
        
        translate([0, depth_support - wall, height_back - (wall)])
            cube([width, wall, wall]);
    }
}

base();
front();
support();
